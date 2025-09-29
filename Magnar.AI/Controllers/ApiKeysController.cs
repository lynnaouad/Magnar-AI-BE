using Magnar.AI.Application.Dto.ApiKeys;
using Magnar.AI.Application.Features.ApiKeys.Commands;
using Magnar.AI.Application.Features.ApiKeys.Queries;
using Magnar.AI.Domain.Entities;
using Microsoft.AspNetCore.OData.Query;
using System.Threading;
using System.Threading.Tasks;

namespace Magnar.AI.Controllers;

public class ApiKeysController : BaseController
{
    public ApiKeysController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetApiKeysQuery(), cancellationToken);
        if (!result.Success)
        {
             return StatusCode(result.StatusCode, result.Errors);
        }

        return Ok(result.Value);
    }

    [HttpGet]
    [Route("odata")]
    public async Task<IActionResult> GetOdata(ODataQueryOptions<ApiKey> filterOptions, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetApiKeysOdataQuery(filterOptions), cancellationToken);
        if (!result.Success)
        {
             return StatusCode(result.StatusCode, result.Errors);
        }

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ApiKeyParametersDto dto, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreateApiKeyCommand(dto), cancellationToken);
        if (!result.Success)
        {
             return StatusCode(result.StatusCode, result.Errors);
        }

        return Ok(result.Value);
    }

    [HttpPut]
    public async Task<IActionResult> Update([FromBody] ApiKeyDto dto, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new UpdateApiKeyCommand(dto), cancellationToken);
        if (!result.Success)
        {
             return StatusCode(result.StatusCode, result.Errors);
        }

        return Ok();
    }

    [HttpDelete]
    [Route("{id}")]
    public async Task<IActionResult> Revoke([FromRoute]int id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new RevokeApiKeyCommand(id), cancellationToken);
        if (!result.Success)
        {
             return StatusCode(result.StatusCode, result.Errors);
        }

        return Ok();
    }
}