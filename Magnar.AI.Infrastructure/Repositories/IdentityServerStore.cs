using Duende.IdentityServer.Models;

namespace Magnar.Recruitment.Infrastructure.Repositories;

public sealed class IdentityServerStore
{
    public static IReadOnlyList<ApiResource> GetApiResources()
    {
        return
        [
            new(Constants.IdentityApi.ApiResourceNames.WebApi)
            {
                ApiSecrets = { new Secret(Constants.IdentityApi.ApiResourceSecret.WebApiKey.Sha256()) },
                UserClaims = { Constants.IdentityApi.ApiClaims.Username, Constants.IdentityApi.ApiClaims.Email },
            },
        ];
    }

    public static IReadOnlyList<IdentityResource> GetIdentityResources()
    {
        return
        [
            new IdentityResources.OpenId(),
            new IdentityResource(Constants.IdentityApi.ApiResourceNames.Custom, [Constants.IdentityApi.ApiClaims.Username, Constants.IdentityApi.ApiClaims.Email])
        ];
    }

    public static IReadOnlyList<ApiScope> GetApiScopes()
    {
        return
        [
            new(Constants.IdentityApi.ApiScopeNames.Full, Constants.IdentityApi.ApiScopeDescriptions.Full),
            new(Constants.IdentityApi.ApiScopeNames.Read, Constants.IdentityApi.ApiScopeDescriptions.Read),
            new(Constants.IdentityApi.ApiScopeNames.Write, Constants.IdentityApi.ApiScopeDescriptions.Write),
            new(Constants.IdentityApi.ApiScopeNames.Modify, Constants.IdentityApi.ApiScopeDescriptions.Modify)
        ];
    }
}
