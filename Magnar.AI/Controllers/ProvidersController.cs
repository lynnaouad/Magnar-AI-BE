using Magnar.AI.Application.Dto.Providers;
using Magnar.AI.Application.Features.Providers.Commands;
using Magnar.AI.Application.Features.Providers.Queries;
using Magnar.AI.Domain.Entities;
using Magnar.AI.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OData.Query;
using System.Threading;
using System.Threading.Tasks;

namespace Magnar.AI.Controllers;

[Authorize]
public class ProvidersController : BaseController
{
    public ProvidersController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetAsync(int id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetProviderQuery(id), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Value);
    }

    [HttpGet]
    [Route("odata")]
    public async Task<IActionResult> GetAllAsync(ODataQueryOptions<Provider> filterOptions, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetProvidersOdataQuery(filterOptions), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] ProviderDto provider, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreateProviderCommand(provider, HttpContext.GetWorkspaceId()), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Value);
    }

    [HttpPut]
    public async Task<IActionResult> UpdateAsync([FromBody] ProviderDto provider, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new UpdateProviderCommand(provider), cancellationToken);
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
        var result = await Mediator.Send(new DeleteProviderCommand(id), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return Ok();
    }

    [HttpPost]
    [Route("test")]
    public async Task<IActionResult> TestProviderAsync([FromBody] ProviderDto provider, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new TestProviderCommand(provider), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Value);
    }
}
