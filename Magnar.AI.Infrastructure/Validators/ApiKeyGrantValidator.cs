using System.Security.Claims;
using Duende.IdentityServer.Models;
using Duende.IdentityServer.Validation;
using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.Recruitment.Infrastructure.Validators;

/// <summary>
/// Custom IdentityServer extension grant validator that enables clients to authenticate
/// using API keys instead of standard OAuth2 grants (e.g., password, client_credentials).
/// 
/// Usage:
///   grant_type=api_key
///   api_key={fullApiKey}
///   scope=scope1 scope2
/// 
/// If valid, this will issue a token with claims bound to the API key.
/// </summary>
public class ApiKeyGrantValidator : IExtensionGrantValidator
{
    private readonly IUnitOfWork unitOfWork;

    public ApiKeyGrantValidator(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    public string GrantType => Constants.IdentityApi.Clients.Api.GrantTypes.ApiKey;

    public async Task ValidateAsync(ExtensionGrantValidationContext context)
    {
        // Extract the raw API key from the request body.
        var rawKey = context.Request.Raw.Get(Constants.IdentityApi.Clients.Api.GrantTypes.ApiKey);
        if (string.IsNullOrWhiteSpace(rawKey))
        {
            context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "missing api_key");
            return;
        }

        // Validate the API key against the repository (check hash, revocation)
        var apiKey = await unitOfWork.ApiKeyRepository.ValidateAsync(rawKey, updateLastUsed: true);
        if (apiKey is null)
        {
            context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "invalid api_key");
            return;
        }

        // Parse requested scopes from the request (space-delimited).
        var requestedScopes = (context.Request.Raw.Get("scope") ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        // Get allowed scopes for this API key.
        var allowed = apiKey.GetScopes().ToHashSet(StringComparer.OrdinalIgnoreCase);

        // If no scopes requested → grant all allowed scopes.
        if (requestedScopes.Length == 0)
        {
            requestedScopes = [.. allowed];
        }       
        else if (requestedScopes.Any(s => !allowed.Contains(s)))
        {
            // If requested scopes contain any not allowed → reject.
            context.Result = new GrantValidationResult(TokenRequestErrors.InvalidScope, "scope not allowed");
            return;
        }

        // Build claims to represent the authenticated subject (service principal).
        var spn = $"spn:ak_{apiKey.PublicId}";

        var user = await unitOfWork.IdentityRepository.GetUserAsync(apiKey.OwnerUserId, default);
        if (user is null)
        {
            context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "invalid user");
            return;
        }

        var claims = new List<Claim>
        {
            new("sub", spn),
            new("api_key_id", apiKey.PublicId),
            new("username", user.UserName),
            new("email", user.Email),
            new(ClaimTypes.NameIdentifier, apiKey.OwnerUserId.ToString()),
            new("tenant_id", apiKey.TenantId),
            new("auth_origin", Constants.IdentityApi.Clients.Api.GrantTypes.ApiKey)
        };

        // Add one "scope" claim per requested scope.
        foreach (var s in requestedScopes)
        {
            claims.Add(new Claim("scope", s));
        }

        // If everything checks out, issue a successful result with claims.
        context.Result = new GrantValidationResult(
            subject: spn,
            authenticationMethod: GrantType,
            claims: claims);
    }
}