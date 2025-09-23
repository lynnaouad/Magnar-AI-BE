using Magnar.AI.Application.Dto.AI;
using Magnar.AI.Controllers;
using Magnar.Recruitment.Application.Features.Dashboard.Commands;
using System.Threading;
using System.Threading.Tasks;

public class PromptsController : BaseController
{
    public PromptsController(IMediator mediator) : base(mediator)
    {
    }

    [HttpPost]
    public async Task<IActionResult> ExecutePrompt([FromBody] PromptDto dto, [FromQuery] int workspaceId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new ExecutePromptCommand(dto, workspaceId), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Value);
    }
}
