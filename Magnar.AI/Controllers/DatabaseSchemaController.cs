using Magnar.AI.Application.Dto.Schema;
using Magnar.AI.Application.Features.DatabaseSchema.Commands;
using Magnar.AI.Application.Features.DatabaseSchema.Queries;
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

    [HttpGet("tables")]
    public async Task<IActionResult> GetTables(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetTablesQuery(), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Value);
    }

    [HttpGet("tables/{schemaName}/{tableName}")]
    public async Task<IActionResult> GetTableInfo(string schemaName, string tableName, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetTableInfoQuery(schemaName, tableName), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Add or update a table block
    /// </summary>
    [HttpPost("annotate")]
    public async Task<IActionResult> AnnotateSchema([FromBody]IEnumerable<TableAnnotationRequest> annotationRequest, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new AnnotateDatabaseSchemaCommand(annotationRequest), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return Ok();
    }

    /// <summary>
    /// Read all blocks as raw text for preview/edit
    /// </summary>
    [HttpGet("selected")]
    public async Task<IActionResult> GetSelectedTable(CancellationToken cancellationToken) 
    {
        var result = await Mediator.Send(new GetSelectedTablesQuery(), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Value);
    }
}
