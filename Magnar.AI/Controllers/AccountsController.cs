using Magnar.AI.Application.Features.Identity.Commands;
using Magnar.AI.Application.Features.Identity.Queries;
using Magnar.AI.Application.Models.Responses;
using Magnar.Recruitment.Application.Features.Identity.Queries;
using Microsoft.AspNetCore.Authorization;

namespace Magnar.AI.Controllers;

[Authorize]
public sealed class AccountsController : BaseController
{
    public AccountsController(IMediator mediator)
        : base(mediator)
    {
    }

    /// <summary>
    /// Authenticate user request.
    /// </summary>
    /// <param name="userCredentials">Authentication credentials (username + password).</param>
    /// <returns><see cref="AuthenticateResponse"/> with a code indicating status of the operation.</returns>
    [AllowAnonymous]
    [HttpPost]
    [Route("access-token")]
    [ProducesResponseType(typeof(AuthenticateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error[]), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetAccessToken([FromBody] UserCredentials userCredentials, CancellationToken cancellationToken)
    {
        Result<AuthenticateResponse> res = await Mediator.Send(new GetAccessTokenQuery(userCredentials), cancellationToken);
        if (!res.Success)
        {
            return BadRequest(res.Errors);
        }

        return Ok(res.Value);
    }

    /// <summary>
    /// replace expired user token.
    /// </summary>
    /// <param name="userCredentials">refresh token.</param>
    /// <returns><see cref="AuthenticateResponse"/> with a code indication status of the operation.</returns>
    [AllowAnonymous]
    [HttpPost]
    [Route("refresh-token")]
    [ProducesResponseType(typeof(AuthenticateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error[]), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RefreshAccessToken([FromBody] UserCredentials userCredentials, CancellationToken cancellationToken)
    {
        Result<AuthenticateResponse> res = await Mediator.Send(new RefreshAccessTokenQuery(userCredentials), cancellationToken);
        if (!res.Success)
        {
            return BadRequest(res.Errors);
        }

        return Ok(res.Value);
    }

    /// <summary>
    /// Get user having <paramref name="userId"/>.
    /// </summary>
    /// <returns>The user having type <see cref="ApplicationUserDto"/>.</returns>
    [HttpGet]
    [Route("users/{userId}")]
    [ProducesResponseType(typeof(ApplicationUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error[]), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUser([FromRoute] int userId, CancellationToken cancellationToken)
    {
        Result<ApplicationUserDto> result = await Mediator.Send(new GetUserQuery(userId), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return Ok(result.Value);
    }

    /// <summary>
    /// Creates a new user with the provided user information.
    /// </summary>
    /// <param name="info">The data to create a new user, provided in the request body.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>
    /// A <see cref="AuthenticateResponse"/> containing the authentication details if the user is successfully created (HTTP 201).
    /// A <see cref="Error[]"/> containing validation or error details if the request is invalid (HTTP 400).
    /// </returns>
    [AllowAnonymous]
    [HttpPost]
    [Route("users")]
    [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(Error[]), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto info, CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new CreateUserCommand(info), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return CreatedAtAction(nameof(GetUser), new { userId = result.Value }, result.Value);
    }

    /// <summary>
    /// Initiates the password recovery process for a user identified by their email address.
    /// </summary>
    /// <param name="email">The email address of the user requesting password recovery.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation if needed.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an IActionResult indicating the outcome of the password recovery request.</returns>
    [AllowAnonymous]
    [HttpPost]
    [Route("users/{email}/password")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error[]), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RecoverPassword([FromRoute] string email, CancellationToken cancellationToken)
    {
        Result result = await Mediator.Send(new RecoverPasswordCommand(email), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return Ok();
    }

    /// <summary>
    /// Resets the password for a user based on the provided reset information.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="info">The data transfer object containing the user's id, new password, and any necessary reset token.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation if needed.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an IActionResult indicating the outcome of the password reset operation.</returns>
    [AllowAnonymous]
    [HttpPatch]
    [Route("users/{userId}/password/reset")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error[]), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ResetPassword([FromRoute] int userId, [FromBody] ResetPasswordDto info, CancellationToken cancellationToken)
    {
        Result result = await Mediator.Send(new ResetPasswordCommand(userId, info), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return Ok();
    }

    /// <summary>
    /// Sends a confirmation email to the user identified by the specified user ID.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="info">The data transfer object containing the user's ID to whom the confirmation email will be sent.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation if needed.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an IActionResult indicating the outcome of the email sending operation.</returns>
    [AllowAnonymous]
    [HttpPost]
    [Route("users/{userId}/email/confirmation")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error[]), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SendConfirmationEmail([FromRoute] int userId, CancellationToken cancellationToken)
    {
        Result result = await Mediator.Send(new SendConfirmationEmailCommand(userId), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return Ok();
    }

    /// <summary>
    /// Confirms the email address of a user based on the provided confirmation information.
    /// </summary>
    /// <param name="userId">The unique identifier of the user.</param>
    /// <param name="info">The data transfer object containing the user's ID and the confirmation token.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation if needed.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains an IActionResult indicating the outcome of the email confirmation operation.</returns>
    [AllowAnonymous]
    [HttpPatch]
    [Route("users/{userId}/email/confirmed")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Error[]), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ConfirmEmail([FromRoute] int userId, [FromBody] ConfirmEmailDto info, CancellationToken cancellationToken)
    {
        Result result = await Mediator.Send(new ConfirmEmailCommand(userId, info), cancellationToken);
        if (!result.Success)
        {
            return BadRequest(result.Errors);
        }

        return Ok();
    }
}