using Magnar.AI.Application.Interfaces.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace Magnar.AI.Authentication;

/// <summary>
/// Custom ASP.NET Core authentication handler that validates API keys sent in the <c>Authorization</c> header.
/// 
/// - Expected format: Authorization: ak_{publicId}.{secretPart}
/// - Uses the <see cref="IApiKeyRepository"/> to validate keys.
/// - If valid, issues a claims principal that can be used for authorization.
/// </summary>
public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationSchemeOptions>
{
    private readonly IUnitOfWork unitOfWork;

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<ApiKeyAuthenticationSchemeOptions> options,
        UrlEncoder encoder,
        ILoggerFactory logger,
        ISystemClock clock,
        IUnitOfWork unitOfWork) : base(options, logger, encoder, clock)
    {
        this.unitOfWork = unitOfWork;
    }

    /// <summary>
    /// Core authentication logic that inspects the request and validates an API key.
    /// </summary>
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // Check if the "Authorization" header is present.
        if (!Request.Headers.TryGetValue("Authorization", out Microsoft.Extensions.Primitives.StringValues value))
        {
            // No header → no authentication result, let other handlers run.
            return AuthenticateResult.NoResult();
        }

        // Ensure it starts with "ak_" (API key prefix).
        var authHeader = value.ToString();
        if (!authHeader.StartsWith("ak_", StringComparison.OrdinalIgnoreCase))
        {
            // Not an API key → let other handlers run.
            return AuthenticateResult.NoResult();
        }

        // Extract the key string (everything after "ak_").
        var apiKey = authHeader["ak_".Length..].Trim();
        if (string.IsNullOrEmpty(apiKey))
        {
            return AuthenticateResult.Fail("Invalid API key format");
        }

        try
        {
            // Validate the API key using repository logic (hash check, revoked/expired, etc.).
            var validatedKey = await unitOfWork.ApiKeyRepository.ValidateAsync(apiKey, updateLastUsed: true);
            if (validatedKey is null)
            {
                return AuthenticateResult.Fail("Invalid API key");
            }

            // Retrieve the owning user linked to this API key.
            var user = await unitOfWork.IdentityRepository.GetUserAsync(validatedKey.OwnerUserId, default);
            if (user is null)
            {
                return AuthenticateResult.Fail("User not found");
            }

            // Build claims identity based on the API key and user information.
            var claims = new List<Claim>
            {
                new("sub", $"spn:ak_{validatedKey.PublicId}"),        
                new("api_key_id", validatedKey.PublicId),            
                new("username", user.UserName),                       
                new("email", user.Email),                     
                new(ClaimTypes.NameIdentifier, validatedKey.OwnerUserId.ToString()), 
                new("tenant_id", validatedKey.TenantId),              
                new("auth_origin", Constants.IdentityApi.Clients.Api.GrantTypes.ApiKey)
            };

            // Add scope claims for all scopes attached to the API key.
            foreach (var scope in validatedKey.GetScopes())
            {
                claims.Add(new Claim("scope", scope));
            }

            // Create authentication ticket (principal + identity).
            var identity = new ClaimsIdentity(claims, Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            // Success: the request is authenticated with this API key.
            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            // Any exception during validation → fail the request.
            return AuthenticateResult.Fail($"API key validation failed: {ex.Message}");
        }
    }
}