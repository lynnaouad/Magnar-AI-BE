using Magnar.AI.Application.Dto.Providers;
using Microsoft.AspNetCore.Http;
using Microsoft.SemanticKernel;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace Magnar.AI.Application.Kernel
{
    public static class KernelPluginFactory
    {
        public static KernelFunction CreateFromMethod(ApiProviderDetails api, ApiProviderAuthDetailsDto authDetails, IHttpClientFactory httpClientFactory)
        {
            try
            {
                return KernelFunctionFactory.CreateFromMethod(
                    (Func<KernelArguments, Task<string>>)(args => ExecuteApiCallAsync(api, authDetails, args, httpClientFactory)),
                       functionName: SanitizeFunctionName(api.FunctionName),
                       description: api.Description,
                       parameters: BuildParameterViews(api)
                   );
            }
            finally { }
            ;
        }

        #region Private Methods

        private static async Task<string> ExecuteApiCallAsync(ApiProviderDetails api, ApiProviderAuthDetailsDto authDetails, KernelArguments args, IHttpClientFactory httpClientFactory)
        {
            try
            {
                var url = api.ApiUrl;
                var method = new HttpMethod(api.HttpMethod.ToString());

                // Deserialize parameters (route/query)
                var parameters = string.IsNullOrWhiteSpace(api.ParametersJson)
                    ? []
                    : JsonConvert.DeserializeObject<List<ApiParameterDto>>(api.ParametersJson) ?? new();

                var queryParams = new List<string>();

                foreach (var p in parameters)
                {
                    if (!args.ContainsName(p.Name))
                    {
                        continue;
                    }
                    ;

                    var value = args[p.Name]?.ToString();

                    switch (p.Location)
                    {
                        case ApiParameterLocation.Route:
                            // Replace placeholder {Name} in the URL if it exists
                            if (url.Contains($"{{{p.Name}}}", StringComparison.OrdinalIgnoreCase))
                            {
                                url = url.Replace($"{{{p.Name}}}", Uri.EscapeDataString(value ?? ""), StringComparison.OrdinalIgnoreCase);
                            }

                            break;

                        case ApiParameterLocation.Query:
                            queryParams.Add($"{p.Name}={Uri.EscapeDataString(value ?? "")}");
                            break;
                    }
                }

                if (queryParams.Count != 0)
                {
                    url += (url.Contains('?') ? '&' : '?') + string.Join('&', queryParams);
                }

                var request = new HttpRequestMessage(method, url);

                if (method == HttpMethod.Post || method == HttpMethod.Put || method == HttpMethod.Patch)
                {
                    string bodyContent;

                    if (!string.IsNullOrWhiteSpace(api.Payload))
                    {
                        try
                        {
                            // interpret Payload as schema JSON
                            var bodyObj = BuildBodyFromSchema(api.Payload, args);
                            bodyContent = bodyObj.ToString(Formatting.None);
                        }
                        catch (Exception)
                        {
                            // fallback: raw payload if schema parsing fails
                            bodyContent = api.Payload;
                        }
                    }
                    else if (args.ContainsName("body"))
                    {
                        // let the caller provide the entire JSON body
                        bodyContent = args["body"]?.ToString() ?? "{}";
                    }
                    else
                    {
                        bodyContent = "{}";
                    }

                    request.Content = new StringContent(
                        bodyContent,
                        Encoding.UTF8,
                        "application/json"
                    );
                }

                var authHeader = await GetAuthHeaderAsync(authDetails, api.ProviderId, httpClientFactory);
                if (authHeader is not null)
                {
                    request.Headers.Authorization = authHeader;
                }

                if(authHeader is null && authDetails.AuthType != AuthType.None)
                {
                    return Constants.Errors.Unauthorized;
                }

                var httpClient = httpClientFactory.CreateClient();

                var response = await httpClient.SendAsync(request);

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
        /// Generates the function parameter metadata for Semantic Kernel from the API definition
        /// </summary>
        private static IEnumerable<KernelParameterMetadata> BuildParameterViews(ApiProviderDetails api)
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
                        Description = $"{p.Description} (This field is: {p.Location.ToString()})",
                        ParameterType = MapToClrType(p.Type),
                        IsRequired = p.Required
                    };
                }
            }

            // Body params
            if (!string.IsNullOrWhiteSpace(api.Payload))
            {
                var schema = JObject.Parse(api.Payload);
                foreach (var prop in schema.Properties())
                {
                    yield return new KernelParameterMetadata(prop.Name)
                    {
                        Description = $"Body field of type {prop.Value}.",
                        ParameterType = typeof(string),
                        IsRequired = true
                    };
                }
            }
        }

        /// <summary>
        /// Maps an ApiParameterDataType to a CLR System.Type used in KernelParameterMetadata.
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
        /// Cleans up the API function name so it’s valid as a Semantic Kernel function identifier.
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
        /// Creates a JSON body (JObject) from a schema definition (schemaJson) and the actual arguments provided
        /// </summary>
        private static JObject BuildBodyFromSchema(string schemaJson, KernelArguments args)
        {
            var schema = JObject.Parse(schemaJson);
            return (JObject)BuildTokenFromSchema(schema, args, parentPath: null);
        }

        /// <summary>
        /// Recursively constructs a JSON token (JToken) based on the schema node type.
        /// If schema node is an object → calls BuildObject.
        /// If schema node is an array → calls BuildArray.
        /// If schema node is a primitive → calls BuildValue.
        /// Provides a generic entry point for schema parsing.
        /// </summary>
        private static JToken BuildTokenFromSchema(JToken schema, KernelArguments args, string? parentPath)
        {
            return schema switch
            {
                JObject obj => BuildObject(obj, args, parentPath),
                JArray arr => BuildArray(arr, args, parentPath),
                JValue val => BuildValue(val, args, parentPath),
                _ => JValue.CreateNull()
            };
        }

        /// <summary>
        /// Builds a JSON object (JObject) by iterating through its properties and populating them with values from KernelArguments.
        /// </summary>
        private static JObject BuildObject(JObject schema, KernelArguments args, string? parentPath)
        {
            var result = new JObject();

            foreach (var prop in schema.Properties())
            {
                var path = string.IsNullOrEmpty(parentPath) ? prop.Name : $"{parentPath}.{prop.Name}";
                var token = BuildTokenFromSchema(prop.Value, args, path);
                if (token != null && token.Type != JTokenType.Null)
                {
                    result[prop.Name] = token;
                }
            }

            return result;
        }

        /// <summary>
        /// Builds a JSON array (JArray) by repeating the item schema for each index found in KernelArguments.
        /// Assumes the first element in schema defines the array item type.
        /// Reads args like items[0], items[1], etc.
        /// Produces a populated JSON array.
        /// </summary>
        private static JArray BuildArray(JArray schema, KernelArguments args, string? parentPath)
        {
            var result = new JArray();

            // assume first element defines the type of array items
            if (schema.Count == 0) return result;

            var itemSchema = schema[0];

            // look for args like "parentPath[0]", "parentPath[1]" ...
            var index = 0;
            while (true)
            {
                var path = $"{parentPath}[{index}]";
                if (!args.ContainsName(path)) break;

                var token = BuildTokenFromSchema(itemSchema, args, path);
                if (token != null && token.Type != JTokenType.Null)
                {
                    result.Add(token);
                }

                index++;
            }

            return result;
        }

        /// <summary>
        /// Builds a primitive JSON value (JValue) from KernelArguments based on the schema type definition.
        /// Handles optional types (e.g., int?).
        /// Validates and parses values into proper types: int, double, decimal, bool, Guid, DateTime, DateTimeOffset, string.
        /// Throws if a required field is missing.
        /// Returns JValue.CreateNull() for invalid or absent values.
        /// </summary>
        private static JToken BuildValue(JValue schemaValue, KernelArguments args, string? path)
        {
            var typeDef = schemaValue.ToString();
            var isOptional = typeDef.EndsWith("?");
            var baseType = isOptional ? typeDef.TrimEnd('?') : typeDef;

            if (string.IsNullOrEmpty(path))
            {
                return JValue.CreateNull();
            }

            if (!args.ContainsName(path))
            {
                if (!isOptional)
                    throw new ArgumentException($"Missing required field: {path}");
                return JValue.CreateNull();
            }

            var raw = args[path]?.ToString();
            if (raw == null)
            {
                return JValue.CreateNull();
            }

            return baseType.ToLowerInvariant() switch
            {
                "int" or "integer" or "number" =>
                    int.TryParse(raw, out var i) ? JToken.FromObject(i) : JValue.CreateNull(),

                "double" or "float" =>
                    double.TryParse(raw, out var d) ? JToken.FromObject(d) : JValue.CreateNull(),

                "decimal" =>
                    decimal.TryParse(raw, out var dec) ? JToken.FromObject(dec) : JValue.CreateNull(),

                "bool" or "boolean" =>
                    bool.TryParse(raw, out var b) ? JToken.FromObject(b) : JValue.CreateNull(),

                "guid" =>
                    Guid.TryParse(raw, out var g) ? JToken.FromObject(g) : JValue.CreateNull(),

                "datetime" or "date" =>
                    DateTime.TryParse(raw, out var dt) ? JToken.FromObject(dt) : JValue.CreateNull(),

                "datetimeoffset" => DateTimeOffset.TryParse(raw, out var dto)
                    ? JToken.FromObject(dto)
                    : JValue.CreateNull(),

                "string" =>
                    JToken.FromObject(raw),

                _ => JToken.FromObject(raw) // fallback
            };
        }

        private static async Task<AuthenticationHeaderValue?> GetAuthHeaderAsync(ApiProviderAuthDetailsDto api, int providerId, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken = default)
        {
            try
            {
                return api.AuthType switch
                {
                    AuthType.None => null,
                    AuthType.PasswordCredentials => new AuthenticationHeaderValue("Bearer", await GetPasswordTokenAsync(api, providerId, httpClientFactory, cancellationToken)),
                    AuthType.ClientCredentials => new AuthenticationHeaderValue("Bearer", await GetClientCredentialsTokenAsync(api, providerId, httpClientFactory, cancellationToken)),
                    _ => null,
                };
            }
            catch (Exception)
            {
                return null;
            }     
        }

        private static async Task<string> GetPasswordTokenAsync(ApiProviderAuthDetailsDto api, int providerId, IHttpClientFactory httpClientFactory, CancellationToken cancellationToken)
        {
            var client = httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, api.TokenUrl!)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    [Constants.IdentityApi.Endpoints.Parameters.GrantType] = Constants.IdentityApi.Clients.RecruitmentApi.GrantTypes.Password,
                    [Constants.IdentityApi.Endpoints.Parameters.UserName] = api.Username!,
                    [Constants.IdentityApi.Endpoints.Parameters.Password] = api.Password!,
                    [Constants.IdentityApi.Endpoints.Parameters.ClientId] = api.ClientId!,
                    [Constants.IdentityApi.Endpoints.Parameters.ClientSecret] = api.ClientSecret!,
                    [Constants.IdentityApi.Endpoints.Parameters.Scope] = api.Scope ?? ""
                })
            };

            var response = await client.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var doc = JsonDocument.Parse(json);

            return doc.RootElement.GetProperty("access_token").GetString()!;
        }

        private static async Task<string> GetClientCredentialsTokenAsync(ApiProviderAuthDetailsDto api, int providerId, IHttpClientFactory httpClientFactory, CancellationToken ct)
        {
            var client = httpClientFactory.CreateClient();
            var request = new HttpRequestMessage(HttpMethod.Post, api.TokenUrl!)
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    [Constants.IdentityApi.Endpoints.Parameters.GrantType] = Constants.IdentityApi.Clients.RecruitmentApi.GrantTypes.ClientCredentials,
                    [Constants.IdentityApi.Endpoints.Parameters.ClientId] = api.ClientId!,
                    [Constants.IdentityApi.Endpoints.Parameters.ClientSecret] = api.ClientSecret!,
                    [Constants.IdentityApi.Endpoints.Parameters.Scope] = api.Scope ?? ""
                })
            };

            var response = await client.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var json = await response.Content.ReadAsStringAsync(ct);
            using var doc = JsonDocument.Parse(json);

            return doc.RootElement.GetProperty("access_token").GetString()!;
        }
        #endregion
    }
}