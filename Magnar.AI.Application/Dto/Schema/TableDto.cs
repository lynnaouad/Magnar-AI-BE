namespace Magnar.AI.Application.Dto.Schema
{
    public class TableDto
    {
        public string SchemaName { get; set; } = string.Empty;

        public string TableName { get; set; } = string.Empty;

        public string FullName { get; set; } = string.Empty;

        public string TableDescription { get; set; } = string.Empty;

        public bool IsSelected { get; set; }

        public List<ColumnInfoDto> Columns { get; set; } = [];
    }
}
