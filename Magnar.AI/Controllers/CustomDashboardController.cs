using Magnar.AI.Application.Dto.Dashboard;
using Magnar.AI.Controllers;
using Magnar.Recruitment.Application.Features.Dashboard.Commands;
using System.Threading;
using System.Threading.Tasks;

[ApiController]
[Route("api/custom-dashboard")]
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
}
