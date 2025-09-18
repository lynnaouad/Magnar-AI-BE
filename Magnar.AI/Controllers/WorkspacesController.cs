using Magnar.AI.Application.Dto.Workspaces;
using Magnar.AI.Application.Features.Providers.Queries;
using Magnar.AI.Application.Features.Workspaces.Commands;
using Microsoft.AspNetCore.Authorization;
using System.Threading;
using System.Threading.Tasks;

namespace Magnar.AI.Controllers;

[Authorize]
public class WorkspacesController : BaseController
{
    public WorkspacesController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetWorkspacesQuery(Username), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] WorkspaceDto workspace, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreateWorkspaceCommand(workspace), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return Ok();
    }

    [HttpPut]
    public async Task<IActionResult> UpdateAsync([FromBody] WorkspaceDto workspace, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new UpdateWorkspaceCommand(workspace), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return Ok();
    }

    [HttpDelete]
    [Route("{id}")]
    public async Task<IActionResult> DeleteAsync(int id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new DeleteWorkspaceCommand(id), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return Ok();
    }

    [HttpGet]
    [Route("{id}/access")]
    public async Task<IActionResult> HaveAccessAsync(int id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new HaveAccessOnWorkspaceQuery(id, Username), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Value);
    }
}
