using Magnar.AI.Application.Dto.Providers;
using System.Linq.Expressions;

namespace Magnar.AI.Application.Interfaces.Repositories;

public interface IProviderRepository : IRepository<Provider>
{

    IRepository<ApiProviderDetails> ApiProviderDetailsRepository { get; }

    Task<ProviderDto> GetDefaultProviderAsync(int workspaceId, ProviderTypes providerType, CancellationToken cancellationToken);

    Task<ProviderDto> GetProviderAsync(int id, CancellationToken cancellationToken);

    Task<IEnumerable<Provider>> GetProvidersAsync(Expression<Func<Provider, bool>> filter, CancellationToken cancellationToken);

    Task<int> CreateProviderAsync(ProviderDto provider, CancellationToken cancellationToken);

    Task UpdateProviderAsync(ProviderDto provider, CancellationToken cancellationToken);

    Task<bool> TestSqlProviderAsync(SqlServerProviderDetailsDto details, CancellationToken cancellationToken);
}
