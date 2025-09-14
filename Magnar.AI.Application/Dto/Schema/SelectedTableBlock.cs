namespace Magnar.AI.Application.Dto.Schema
{
    public class SelectedTableBlock
    {
        public string RawBlockText { get; set; } = string.Empty;

        public string FullTableName { get; set; } = string.Empty;

        public bool HasComments { get; set; }
    }
}
