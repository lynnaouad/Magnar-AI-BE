using Magnar.AI.Application.Dto.AI;
using Magnar.AI.Controllers;
using Magnar.AI.Extensions;
using Magnar.Recruitment.Application.Features.Dashboard.Commands;
using Microsoft.AspNetCore.Authorization;
using System.Threading;
using System.Threading.Tasks;

[Authorize]
[ApiController]
public class PromptsController : BaseController
{
    public PromptsController(IMediator mediator) : base(mediator)
    {
    }

    [HttpPost]
    public async Task<IActionResult> ExecutePrompt([FromBody] PromptDto dto, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new ExecutePromptCommand(dto, HttpContext.GetWorkspaceId()), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Value);
    }
}
