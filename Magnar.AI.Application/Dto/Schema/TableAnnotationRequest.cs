namespace Magnar.AI.Application.Dto.Schema
{
    public class TableAnnotationRequest
    {
        public string FullTableName { get; set; } = string.Empty;

        public string? TableDescription { get; set; }

        public Dictionary<string, string?> ColumnComments { get; set; } = [];
    }
}
