using Magnar.AI.Application.Dto.Schema;

namespace Magnar.AI.Application.Interfaces.Managers
{
    public interface ISchemaManager
    {
        /// <summary>
        /// Retrieves all user-defined tables from the default SQL Server connection.
        /// </summary>
        Task<Result<IEnumerable<TableDto>>> GetTablesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Retrieves detailed information about a specific table from SQL Server,
        /// including its columns (with PK/nullable info) and foreign keys.
        /// </summary>
        Task<Result<TableInfoDto>> GetTableInfoAsync(string schema, string table, CancellationToken cancellationToken = default);
    }
}
