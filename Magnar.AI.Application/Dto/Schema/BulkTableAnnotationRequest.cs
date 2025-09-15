namespace Magnar.AI.Application.Dto.Schema
{
    public class BulkTableAnnotationRequest
    {
        public IEnumerable<TableAnnotationRequest> Tables { get; set; } = [];
    }
}
