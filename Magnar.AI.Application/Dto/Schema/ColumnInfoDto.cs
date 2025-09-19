namespace Magnar.AI.Application.Dto.Schema
{
    public class ColumnInfoDto
    {
        public string ColumnName { get; set; } = string.Empty;

        public string DataType { get; set; } = string.Empty;

        public bool IsNullable { get; set; }

        public bool IsPrimaryKey { get; set; }

        public bool IsForeignKey { get; set; }

        public string ColumnDescription { get; set; } = string.Empty;

        public string ForeignKeyReferencedTable { get; set; } = string.Empty;
    }
}
