using Magnar.AI.Application.Dto.Providers;
using Magnar.AI.Application.Dto.Schema;
using Magnar.AI.Application.Features.DatabaseSchema.Commands;
using Magnar.AI.Application.Features.DatabaseSchema.Queries;
using Magnar.AI.Extensions;
using Microsoft.AspNetCore.Authorization;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Magnar.AI.Controllers;

[Authorize]
public class DatabaseSchemaController : BaseController
{
    public DatabaseSchemaController(IMediator mediator) : base(mediator)
    {
    }

    [HttpPost("tables")]
    public async Task<IActionResult> LoadTablesFromDatabase([FromBody] ProviderDto provider, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetTablesQuery(provider), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Value);
    }

    [HttpGet("selected")]
    public async Task<IActionResult> LoadTablesFromFile(int providerId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetSelectedTablesQuery(HttpContext.GetWorkspaceId(), providerId), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Value);
    }

    [HttpPost("annotate")]
    public async Task<IActionResult> AnnotateSchema([FromBody]IEnumerable<TableDto> selectedTables, int providerId, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new AnnotateDatabaseSchemaCommand(selectedTables, HttpContext.GetWorkspaceId(), providerId), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return Ok();
    }
}
