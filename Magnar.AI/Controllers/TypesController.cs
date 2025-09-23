using Magnar.AI.Application.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Magnar.AI.Controllers;

public sealed class TypesController : BaseController
{
    public TypesController(IMediator mediator) : base(mediator)
    {
    }

    /// <summary>
    /// Retrieves a list of values for a specified enum type.
    /// </summary>
    /// <param name="enumType">The name of the enum type.</param>
    /// <returns>A list of enum values.</returns>
    /// <response code="200">Returns the list of enum values.</response>
    /// <response code="400">If the provided enum type is invalid.</response>
    [AllowAnonymous]
    [HttpGet]
    [Route("{enumType}")]
    [ProducesResponseType(typeof(IEnumerable<EnumDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult GetEnumValues([FromRoute] string enumType)
    {
        if (string.IsNullOrWhiteSpace(enumType))
        {
            return BadRequest();
        }

        Type? type = AppDomain.CurrentDomain
           .GetAssemblies()
           .SelectMany(a => a.GetTypes())
           .FirstOrDefault(t => t.Name.Equals(enumType, StringComparison.OrdinalIgnoreCase) && t.IsEnum);

        return type is null || !type.IsEnum ? BadRequest() : Ok(Utilities.GetEnumValues(type));
    }
}
