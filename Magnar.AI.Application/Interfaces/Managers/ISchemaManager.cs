using Magnar.AI.Application.Dto.Providers;
using Magnar.AI.Application.Dto.Schema;

namespace Magnar.AI.Application.Interfaces.Managers
{
    public interface ISchemaManager
    {
        /// <summary>
        /// Retrieves all user-defined tables from the default SQL Server connection.
        /// </summary>
        Task<Result<IEnumerable<TableDto>>> LoadTablesFromDatabaseAsync(SqlServerProviderDetailsDto connection, CancellationToken cancellationToken = default);

        /// <summary>
        /// returns only selected tables after merge
        /// </summary>
        Task<List<TableDto>> LoadFromFileAsync(int workspaceId, int providerId, CancellationToken cancellationToken = default);

        Task UpsertFileAsync(IEnumerable<TableDto> incoming, int workspaceId, int providerId, CancellationToken cancellationToken = default);

        Task SaveAsync(IEnumerable<TableDto> tables, string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Remove tables that no longer exist in the DB (strict compare on [schema].[table]).
        /// Returns the number of removed entries.
        /// </summary>
        Task<int> RemoveMissingTablesAsync(int workspaceId, int providerId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Merge live DB tables with file selections. 
        /// - Sets IsSelected based on file (true/false; default false if not present).
        /// - Optionally carries over TableDescription and Column.Description from file.
        /// - Keeps DB truth for datatypes, PK/FK, nullability, etc.
        /// Returns the merged live list.
        /// </summary>
        Task<IEnumerable<TableDto>> MergeSelectionsFromFileAsync(int workspaceId, int providerId, CancellationToken cancellationToken = default);
    }
}
