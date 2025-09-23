namespace Magnar.AI.Application.Dto.Dashboard;

public class DashboardPromptDto
{
    public string Prompt { get; set; } = string.Empty;

    public DashboardTypes ChartType { get; set; } = DashboardTypes.Pie;

    public int WorkspaceId { get; set; }
}
