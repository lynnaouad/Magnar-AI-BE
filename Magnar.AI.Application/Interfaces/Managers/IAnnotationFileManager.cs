using Magnar.AI.Application.Dto.Schema;

namespace Magnar.AI.Application.Interfaces.Managers
{
    public interface IAnnotationFileManager
    {
        /// <summary>
        /// Appends a new annotation block for a table, or replaces the existing block if it already exists.
        /// </summary>
        Task AppendOrReplaceBlocksAsync(IEnumerable<TableAnnotationRequest> requests, int connectionId);

        /// <summary>
        /// Reads all annotation blocks for a specific connection from its file.
        /// Each block corresponds to one table's metadata and comments.
        /// </summary>
        Task<IEnumerable<SelectedTableBlock>> ReadAllBlocksAsync(int connectionId);

        /// <summary>
        /// Removes annotation blocks for tables that no longer exist in the database.
        /// </summary>
        Task CleanupOrphanedBlocksAsync(int connectionId, IEnumerable<string> existingDbTables);
    }
}
