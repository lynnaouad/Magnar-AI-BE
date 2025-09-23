using Magnar.AI.Application.Features.ApiKeys.Commands;
using Magnar.AI.Application.Features.ApiKeys.Queries;
using System.Threading;
using System.Threading.Tasks;

namespace Magnar.AI.Controllers;

public class ApiKeysController : BaseController
{
    public ApiKeysController(IMediator mediator) : base(mediator)
    {
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateApiKeyDto request, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreateApiKeyCommand(request), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Value);
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetApiKeysQuery(), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Value);
    }

    [HttpDelete]
    [Route("{publicId}")]
    public async Task<IActionResult> Revoke(string publicId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new RevokeApiKeyCommand(publicId), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return Ok();
    }
}