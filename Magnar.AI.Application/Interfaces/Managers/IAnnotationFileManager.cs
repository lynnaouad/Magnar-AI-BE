using Magnar.AI.Application.Dto.Schema;

namespace Magnar.AI.Application.Interfaces.Managers
{
    public interface IAnnotationFileManager
    {
        public Task AppendOrReplaceBlockAsync(TableAnnotationRequest req, int connectionId);

        public Task<IEnumerable<SelectedTableBlock>> ReadAllBlocksAsync(int connectionId);
    }
}
