namespace Magnar.AI.Application.Dto.Schema
{
    public class TableDto
    {
        public string SchemaName { get; set; } = string.Empty;

        public string TableName { get; set; } = string.Empty;

        public string FullName => $"[{SchemaName}].[{TableName}]";
    }
}
