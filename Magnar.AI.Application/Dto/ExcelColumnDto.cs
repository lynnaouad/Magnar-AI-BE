namespace Magnar.AI.Application.Dto;

public sealed class ExcelColumnDto
{
    public string Header { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public double Width { get; set; } = 20;
}
