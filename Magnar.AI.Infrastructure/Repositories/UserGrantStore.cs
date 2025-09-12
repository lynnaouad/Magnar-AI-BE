using Duende.IdentityServer.Models;
using Duende.IdentityServer.Stores;
using Magnar.AI.Infrastructure.Persistence.Contexts;

namespace Magnar.AI.Infrastructure.Repositories;

public sealed class UserGrantStore : IPersistedGrantStore
{
    private readonly MagnarAIDbContext _context;

    public UserGrantStore(MagnarAIDbContext context)
    {
        _context = context;
    }

    public async Task StoreAsync(PersistedGrant grant)
    {
        await _context.UserGrants.AddAsync(grant);
        await _context.SaveChangesAsync();
    }

    public async Task<PersistedGrant?> GetAsync(string key)
    {
        return await _context
            .UserGrants
            .Where(grant => grant.Key == key)
            .FirstOrDefaultAsync() ?? throw new UnauthorizedAccessException();
    }

    public async Task<IEnumerable<PersistedGrant>> GetAllAsync(PersistedGrantFilter filter)
    {
        return await _context
            .UserGrants
            .Where(grant =>
                (filter.SubjectId == null || grant.SubjectId == filter.SubjectId) &&
                (filter.ClientId == null || grant.ClientId == filter.ClientId) &&
                (filter.ClientIds == null || filter.ClientIds.Contains(grant.ClientId)) &&
                (filter.Type == null || grant.Type == filter.Type) &&
                (filter.Types == null || filter.Types.Contains(grant.Type)))
            .ToListAsync();
    }

    public async Task RemoveAsync(string key)
    {
        var grant = _context.UserGrants.FirstOrDefault(grant => grant.Key == key);
        if (grant is not null)
        {
            _context.UserGrants.Remove(grant);
            await _context.SaveChangesAsync();
        }
    }

    public async Task RemoveAllAsync(PersistedGrantFilter filter)
    {
        var grants = _context
            .UserGrants
            .Where(grant =>
                (filter.SubjectId == null || grant.SubjectId == filter.SubjectId) &&
                (filter.ClientId == null || grant.ClientId == filter.ClientId) &&
                (filter.ClientIds == null || filter.ClientIds.Contains(grant.ClientId)) &&
                (filter.Type == null || grant.Type == filter.Type) &&
                (filter.Types == null || filter.Types.Contains(grant.Type)));

        if (grants.Any())
        {
            _context.UserGrants.RemoveRange(grants);
            await _context.SaveChangesAsync();
        }
    }
}
