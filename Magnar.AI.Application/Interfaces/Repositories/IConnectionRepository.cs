using Magnar.AI.Application.Dto.Connection;
using Magnar.AI.Application.Models.Responses;
using Microsoft.AspNetCore.OData.Query;

namespace Magnar.AI.Application.Interfaces.Repositories;

public interface IConnectionRepository : IRepository<Connection>
{
    Task<ConnectionDto> GetDefaultConnectionAsync(CancellationToken cancellationToken);

    Task<bool> TestSqlConnectionAsync(SqlServerConnectionDetailsDto details, CancellationToken cancellationToken);

    string BuildSqlServerConnectionString(SqlServerConnectionDetailsDto details);

    string ProtectPassword(string password);

    string UnprotectPassword(string protectedPassword);

    Task<ConnectionDto> GetConnectionAsync(int id, CancellationToken cancellationToken);

    Task<OdataResponse<ConnectionDto>> GetConnectionsOdataAsync(ODataQueryOptions<Connection> filterOptions, CancellationToken cancellationToken);
}
