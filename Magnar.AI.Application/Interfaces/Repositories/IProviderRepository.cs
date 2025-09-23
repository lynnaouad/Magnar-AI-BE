using Magnar.AI.Application.Dto.Providers;
using Magnar.AI.Application.Models.Responses;
using Microsoft.AspNetCore.OData.Query;

namespace Magnar.AI.Application.Interfaces.Repositories;

public interface IProviderRepository : IRepository<Provider>
{

    IRepository<ApiProviderDetails> ApiProviderDetailsRepository { get; }

    Task<ProviderDto> GetDefaultProviderAsync(int workspaceId, ProviderTypes providerType, CancellationToken cancellationToken);

    Task<ProviderDto> GetProviderAsync(int id, CancellationToken cancellationToken);

    Task<OdataResponse<ProviderDto>> GetProvidersOdataAsync(ODataQueryOptions<Provider> filterOptions, CancellationToken cancellationToken);

    Task<bool> TestSqlProviderAsync(SqlServerProviderDetailsDto details, CancellationToken cancellationToken);

    string BuildSqlServerConnectionString(SqlServerProviderDetailsDto details);
}
