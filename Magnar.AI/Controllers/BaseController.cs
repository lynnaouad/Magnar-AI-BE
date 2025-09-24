using Magnar.AI.Extensions;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using static Magnar.AI.Static.Constants;

namespace Magnar.AI.Controllers;

[Authorize]
[Route("api/[controller]")]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
public abstract class BaseController : ControllerBase
{
    protected BaseController(IMediator mediator)
    {
        Mediator = mediator;
    }

    protected string Username { get => User?.FindFirstValue(IdentityApi.ApiClaims.Username) ?? string.Empty; }

    protected IMediator Mediator { get; }
}
