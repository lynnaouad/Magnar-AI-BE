using Magnar.AI.Application.Dto.Connection;
using Magnar.AI.Application.Features.Connection.Commands;
using Magnar.AI.Application.Features.Connection.Queries;
using Microsoft.AspNetCore.OData.Query;
using System.Threading;
using System.Threading.Tasks;

namespace Magnar.AI.Controllers;

public class ConnectionsController : BaseController
{
    public ConnectionsController(IMediator mediator) : base(mediator)
    {
    }

    [HttpGet]
    [Route("{id}")]
    public async Task<IActionResult> GetAsync(int id, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetConnectionQuery(id), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Value);
    }

    [HttpGet]
    [Route("odata")]
    public async Task<IActionResult> GetAllAsync(ODataQueryOptions<Domain.Entities.Connection> filterOptions, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetConnectionsOdataQuery(filterOptions), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Value);
    }

    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] ConnectionDto connection, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreateConnectionCommand(connection), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return Ok();
    }

    [HttpPut]
    public async Task<IActionResult> UpdateAsync([FromBody] ConnectionDto connection, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new UpdateConnectionCommand(connection), cancellationToken);
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
        var result = await Mediator.Send(new DeleteConnectionCommand(id), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return Ok();
    }

    [HttpPost]
    [Route("test")]
    public async Task<IActionResult> TestConnectionAsync([FromBody] ConnectionDto connection, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new TestConnectionCommand(connection), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Value);
    }
}
