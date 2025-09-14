namespace Magnar.AI.Application.Dto.Schema
{
    public class ForeignKeyInfoDto
    {
        public string ColumnName { get; set; } = string.Empty;  

        public string ReferencedSchema { get; set; } = string.Empty;

        public string ReferencedTable { get; set; } = string.Empty;

        public string ReferencedColumn { get; set; } = string.Empty;
    }
}
