using Magnar.AI.Application.Dto.Dashboard;
using Magnar.AI.Controllers;
using Magnar.Recruitment.Application.Features.Dashboard.Commands;
using System.Threading;
using System.Threading.Tasks;

public class CustomDashboardController : BaseController
{
    public CustomDashboardController(IMediator mediator) : base(mediator)
    {
    }

    [HttpPost]
    public async Task<IActionResult> GenerateDashboard([FromBody] DashboardPromptDto dto, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GenerateDashboardCommand(dto), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new { dashboardId = result.Value });
    }

    [HttpPost]
    [Route("change-type")]
    public async Task<IActionResult> ChangeDashboardType([FromBody] DashboardPromptDto dto, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new ChangeDashboardTypeCommand(dto), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return Ok(new { dashboardId = result.Value });
    }
}
