namespace Magnar.AI.Controllers;

[Route("api/[controller]")]
[ApiController]
[Produces(MediaTypeNames.Application.Json)]
public abstract class BaseController : ControllerBase
{
    protected BaseController(IMediator mediator)
    {
        Mediator = mediator;
    }

    protected string Username { get => User?.Identity?.Name ?? string.Empty; }

    protected IMediator Mediator { get; }
}
