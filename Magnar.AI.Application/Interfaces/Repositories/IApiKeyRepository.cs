namespace Magnar.AI.Application.Interfaces.Repositories;

public interface IApiKeyRepository : IRepository<ApiKey>
{
    Task<(string plainKey, ApiKey entity)> CreateAsync(int ownerUserId, string tenantId, IEnumerable<string> scopes, TimeSpan? lifetime, string name, string metadataJson);

    Task<ApiKey> ValidateAsync(string fullKey, bool updateLastUsed = true);

    Task<List<ApiKey>> ListAsync(int ownerUserId, string tenantId);

    Task<bool> RevokeAsync(string publicId, int ownerUserId, string tenantId, CancellationToken cancellationToken);
}
