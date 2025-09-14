namespace Magnar.AI.Application.Dto.Schema
{
    public class TableInfoDto : TableDto
    {
        public IEnumerable<ColumnInfoDto> Columns { get; set; } = [];
       
        public IEnumerable<ForeignKeyInfoDto> ForeignKeys { get; set; } = [];
    }
}
