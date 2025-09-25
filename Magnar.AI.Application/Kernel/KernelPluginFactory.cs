using Duende.IdentityServer.Models;
using Magnar.AI.Application.Dto.Providers;
using Magnar.AI.Application.Features.DatabaseSchema.Commands;
using Magnar.AI.Application.Helpers;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Domain.Entities;
using Microsoft.SemanticKernel;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Magnar.AI.Application.Kernel
{
    /// <summary>
    /// Factory for creating Semantic Kernel plugin functions, either from API providers or a default SQL generator fallback.
    /// </summary>
    public static class KernelPluginFactory
    {
        /// <summary>
        /// Creates a Semantic Kernel function that wraps an external API call.
        /// </summary>
        /// <param name="api">API provider details (endpoint, method, parameters).</param>
        /// <param name="authDetails">Authentication details for the API provider.</param>
        /// <param name="httpClientFactory">Factory for creating HttpClient instances.</param>
        /// <returns>A KernelFunction that invokes the external API when called.</returns>
        public static KernelFunction CreateApiFunction(ApiProviderDetails api, ApiProviderAuthDetailsDto authDetails, IHttpClientFactory httpClientFactory, ICookieSessionStore cookieStore)
        {
            try
            {
                return KernelFunctionFactory.CreateFromMethod(
                    (Func<KernelArguments, Task<string>>)(args => ExecuteApiFunction(api, authDetails, args, httpClientFactory, cookieStore)),
                       functionName: SanitizeFunctionName(api.FunctionName),
                       description: api.Description,
                       parameters: BuildParameters(api)
                   );
            }
            finally { };
        }

        /// <summary>
        /// Creates a fallback Semantic Kernel function that generates and executes
        /// a SQL query based on the current schema and user prompt.
        /// </summary>
        /// <param name="workspaceId">Workspace identifier for schema context.</param>
        /// <param name="mediator">Mediator instance used to dispatch the SQL generation command.</param>
        /// <returns>A KernelFunction that produces SQL and executes it when no plugin matches.</returns>
        public static KernelFunction CreateFallbackSqlFunction(int workspaceId, IMediator mediator)
        {
            try
            {
                return KernelFunctionFactory.CreateFromMethod(
                    (Func<KernelArguments, Task<string>>)(args => ExecuteFallbackSqlFunction(workspaceId, args, mediator)),
                       functionName: Constants.KernelFunctionNames.DefaultQueryGenerator,
                       description: "Generates and executes a SQL query based on schema and user prompt if no plugin matches.",
                       parameters:
                        [
                            new KernelParameterMetadata("prompt")
                            {
                                Description = "User request prompt in plain text",
                                ParameterType = typeof(string)
                            }
                        ]
                   );
            }
            finally { };
        }

        #region Private Methods

        /// <summary>
        /// Executes an API call using the provided API definition, authentication, and arguments.
        /// Handles query parameters, route parameters, and request body.
        /// </summary>
        private static async Task<string> ExecuteApiFunction(ApiProviderDetails api, ApiProviderAuthDetailsDto authDetails, KernelArguments args, IHttpClientFactory httpClientFactory, ICookieSessionStore cookieStore)
        {
            try
            {
                var httpClient = authDetails.AuthType == AuthType.CookieSession
                    ? cookieStore.CreateClientWithCookies(api.ProviderId)
                    : httpClientFactory.CreateClient();

                // build request (first attempt)
                var request = BuildRequest(api, args);

                // add Authorization header if needed
                var authHeader = await GetAuthHeaderAsync(authDetails, httpClientFactory);
                if (authHeader is not null)
                {
                    request.Headers.Authorization = authHeader;
                }

                var response = await httpClient.SendAsync(request);

                // Retry if unauthorized and cookie-based
                if (authDetails.AuthType == AuthType.CookieSession && response.StatusCode == HttpStatusCode.Unauthorized)
                {
                    // Replace expired sessions
                    cookieStore.Refresh(api.ProviderId);
                    httpClient = cookieStore.CreateClientWithCookies(api.ProviderId);

                    if (await TryCookieLoginAsync(authDetails, httpClient))
                    {
                        // rebuild request for retry
                        var retryRequest = BuildRequest(api, args);
                        response = await httpClient.SendAsync(retryRequest);
                    }
                }

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync();
                }

                return response.StatusCode.ToString();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
        }

        /// <summary>
        /// Executes the fallback SQL assistant: generates a SQL query from a user prompt
        /// and executes it via the mediator pipeline.
        /// </summary>
        private static async Task<string> ExecuteFallbackSqlFunction(int workspaceId, KernelArguments args, IMediator mediator)
        {
            var prompt = args["prompt"]?.ToString() ?? string.Empty;

            var result = await mediator.Send(new GenerateAndExecuteSqlQueryCommand(prompt, workspaceId), default);

            return result.Value;
        }

        private static HttpRequestMessage BuildRequest(ApiProviderDetails api, KernelArguments args)
        {
            var url = api.ApiUrl;
            var method = new HttpMethod(api.HttpMethod.ToString());

            // Route & query params
            var parameters = string.IsNullOrWhiteSpace(api.ParametersJson)
                ? []
                : JsonConvert.DeserializeObject<List<ApiParameterDto>>(api.ParametersJson) ?? new();

            var queryParams = new List<string>();

            foreach (var p in parameters)
            {
                if (!args.ContainsName(p.Name)) continue;

                var value = args[p.Name]?.ToString();

                switch (p.Location)
                {
                    case ApiParameterLocation.Route:
                        if (url.Contains($"{{{p.Name}}}", StringComparison.OrdinalIgnoreCase))
                            url = url.Replace($"{{{p.Name}}}", Uri.EscapeDataString(value ?? ""), StringComparison.OrdinalIgnoreCase);
                        break;

                    case ApiParameterLocation.Query:
                        queryParams.Add($"{p.Name}={Uri.EscapeDataString(value ?? "")}");
                        break;
                }
            }

            if (queryParams.Count != 0)
                url += (url.Contains('?') ? '&' : '?') + string.Join('&', queryParams);

            var request = new HttpRequestMessage(method, url);

            // Body payload
            if (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch)
            {
                var bodyContent = args.ContainsName("body") ? args["body"]?.ToString() ?? "{}" : "{}";
                request.Content = new StringContent(bodyContent, Encoding.UTF8, "application/json");
            }

            return request;
        }

        /// <summary>
        /// Generates Semantic Kernel parameter metadata for each API parameter,
        /// including route, query, and body parameters.
        /// </summary>
        private static IEnumerable<KernelParameterMetadata> BuildParameters(ApiProviderDetails api)
        {
            if (!string.IsNullOrWhiteSpace(api.ParametersJson))
            {
                var parameters = JsonConvert.DeserializeObject<List<ApiParameterDto>>(api.ParametersJson);
                if (parameters == null)
                {
                    yield break;
                }

                foreach (var p in parameters)
                {
                    yield return new KernelParameterMetadata(p.Name)
                    {
                        Description = $"{p.Description} (This field is: {p.Location})",
                        ParameterType = MapToClrType(p.Type),
                        IsRequired = p.Required
                    };
                }
            }

            // Body params
            if (!string.IsNullOrWhiteSpace(api.Payload))
            {
                yield return new KernelParameterMetadata("body")
                {
                    Description = $"JSON payload for {api.FunctionName}. Schema: {api.Payload}",
                    ParameterType = typeof(string),
                    IsRequired = true
                };
            }
        }

        /// <summary>
        /// Maps custom API parameter data types to CLR types
        /// for use in KernelParameterMetadata.
        /// </summary>
        private static Type MapToClrType(ApiParameterDataType type) => type switch
        {
            ApiParameterDataType.String => typeof(string),
            ApiParameterDataType.Integer => typeof(int),
            ApiParameterDataType.Double => typeof(double),
            ApiParameterDataType.Float => typeof(float),
            ApiParameterDataType.Decimal => typeof(decimal),
            ApiParameterDataType.Boolean => typeof(bool),
            ApiParameterDataType.DateTime => typeof(DateTime),
            ApiParameterDataType.DateTimeOffset => typeof(DateTimeOffset),
            ApiParameterDataType.Guid => typeof(Guid),
            ApiParameterDataType.Enum => typeof(string),
            ApiParameterDataType.Array => typeof(string[]),
            _ => typeof(string)
        };

        /// <summary>
        /// Sanitizes a function name so that it is valid as a Semantic Kernel identifier.
        /// Invalid characters are replaced with underscores.
        /// </summary>
        private static string SanitizeFunctionName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return string.Empty;

            var chars = name.Select(c =>
                (c >= 'a' && c <= 'z') ||
                (c >= 'A' && c <= 'Z') ||
                (c >= '0' && c <= '9') ||
                c == '_'
                    ? c
                    : '_'
            );

            return new string(chars.ToArray());
        }

        /// <summary>
        /// Resolves an authentication header for the given API provider
        /// based on its configured authentication type.
        /// </summary>
        private static async Task<AuthenticationHeaderValue?> GetAuthHeaderAsync(ApiProviderAuthDetailsDto api, IHttpClientFactory httpClientFactory)
        {
            try
            {
                return api.AuthType switch
                {
                    AuthType.NoAuth => null,
                    AuthType.PasswordCredentials => new AuthenticationHeaderValue("Bearer", await GetPasswordTokenAsync(api, httpClientFactory)),
                    AuthType.ClientCredentials => new AuthenticationHeaderValue("Bearer", await GetClientCredentialsTokenAsync(api, httpClientFactory)),
                    AuthType.ApiKey => BuildApiKeyHeader(api),
                    _ => null,
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Retrieves an OAuth token using password grant flow for an API provider.
        /// </summary>
        private static async Task<string> GetPasswordTokenAsync(ApiProviderAuthDetailsDto api, IHttpClientFactory httpClientFactory)
        {
            var client = httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, api.TokenUrl!)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    [Constants.IdentityApi.Endpoints.Parameters.GrantType] = Constants.IdentityApi.Clients.Api.GrantTypes.Password,
                    [Constants.IdentityApi.Endpoints.Parameters.UserName] = api.Username!,
                    [Constants.IdentityApi.Endpoints.Parameters.Password] = api.Password!,
                    [Constants.IdentityApi.Endpoints.Parameters.ClientId] = api.ClientId!,
                    [Constants.IdentityApi.Endpoints.Parameters.ClientSecret] = api.ClientSecret!,
                    [Constants.IdentityApi.Endpoints.Parameters.Scope] = api.Scope ?? ""
                })
            };

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            return doc.RootElement.GetProperty("access_token").GetString()!;
        }

        /// <summary>
        /// Retrieves an OAuth token using client credentials flow for an API provider.
        /// </summary>
        private static async Task<string> GetClientCredentialsTokenAsync(ApiProviderAuthDetailsDto api, IHttpClientFactory httpClientFactory)
        {
            var client = httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, api.TokenUrl!)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    [Constants.IdentityApi.Endpoints.Parameters.GrantType] = Constants.IdentityApi.Clients.Api.GrantTypes.ClientCredentials,
                    [Constants.IdentityApi.Endpoints.Parameters.ClientId] = api.ClientId!,
                    [Constants.IdentityApi.Endpoints.Parameters.ClientSecret] = api.ClientSecret!,
                    [Constants.IdentityApi.Endpoints.Parameters.Scope] = api.Scope ?? ""
                })
            };

            var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);

            return doc.RootElement.GetProperty("access_token").GetString()!;
        }

        private static async Task<bool> TryCookieLoginAsync(ApiProviderAuthDetailsDto authDetails, HttpClient httpClient)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, authDetails?.TokenUrl?.TrimEnd('/'))
            {
                Content = new StringContent(authDetails?.Payload ?? string.Empty, Encoding.UTF8, "application/json")
            };

            var response = await httpClient.SendAsync(request);

            return response.IsSuccessStatusCode;
        }

        private static AuthenticationHeaderValue? BuildApiKeyHeader(ApiProviderAuthDetailsDto api)
        {
            if (string.IsNullOrWhiteSpace(api.ApiKeyValue))
            {
                return null;
            }
               
            return new AuthenticationHeaderValue(api.ApiKeyName, api.ApiKeyValue);
        }

        #endregion
    }
}