using Magnar.AI.Application.Dto.Schema;

namespace Magnar.AI.Application.Interfaces.Managers
{
    public interface ISchemaManager
    {
        Task<Result<IEnumerable<TableDto>>> GetTablesAsync(CancellationToken cancellationToken = default);
        
        Task<Result<TableInfoDto>> GetTableInfoAsync(string schema, string table, CancellationToken cancellationToken = default);
    }
}
