using Magnar.AI.Application.Dto.Providers;
using Magnar.AI.Application.Models.Responses;
using Microsoft.AspNetCore.OData.Query;

namespace Magnar.AI.Application.Interfaces.Repositories;

public interface IProviderRepository : IRepository<Provider>
{
    Task<bool> TestSqlProviderAsync(SqlServerProviderDetailsDto details, CancellationToken cancellationToken);

    string BuildSqlServerConnectionString(SqlServerProviderDetailsDto details);

    string ProtectPassword(string password);

    string UnprotectPassword(string protectedPassword);

    Task<ProviderDto> GetProviderAsync(int id, CancellationToken cancellationToken);

    Task<OdataResponse<ProviderDto>> GetProvidersOdataAsync(ODataQueryOptions<Provider> filterOptions, CancellationToken cancellationToken);
}
