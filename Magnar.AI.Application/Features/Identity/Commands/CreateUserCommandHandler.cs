using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Features.Identity.Notifications;
using Microsoft.AspNetCore.Identity;
using Serilog;

namespace Magnar.AI.Application.Features.Identity.Commands;

public sealed record CreateUserCommand(CreateUserDto Info) : IRequest<Result<int>>;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, Result<int>>
{
    private readonly IMapper mapper;
    private readonly IReCaptchaService reCaptchaService;
    private readonly IMediator mediator;
    private readonly IUnitOfWork unitOfWork;

    public CreateUserCommandHandler(
        IMapper mapper,
        IReCaptchaService reCaptchaService,
        IUnitOfWork unitOfWork,
        IMediator mediator)
    {
        this.mapper = mapper;
        this.reCaptchaService = reCaptchaService;
        this.mediator = mediator;
        this.unitOfWork = unitOfWork;
    }

    public async Task<Result<int>> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        // Check recaptcha token
        if (request.Info.ApplicationUserDto.ReCaptchaTokenEnabled && !await reCaptchaService.ValidateReCaptchaTokenAsync(request.Info.ApplicationUserDto.ReCaptchaToken))
        {
            return Result<int>.CreateFailure([new(Constants.Errors.CheckReCaptcha)]);
        }

        (bool success, Error? error) = await ValidateUser(request.Info.ApplicationUserDto.Username, request.Info.ApplicationUserDto.Email, cancellationToken);
        if (!success)
        {
            return Result<int>.CreateFailure(error is not null ? [error] : []);
        }

        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);

            ApplicationUser newUser = mapper.Map<ApplicationUser>(request.Info.ApplicationUserDto);

            IdentityResult result = await unitOfWork.IdentityRepository.CreateUserAsync(
                newUser,
                request.Info.ApplicationUserDto.Password,
                cancellationToken);

            if (!result.Succeeded)
            {
                throw new Exception(result.Errors?.FirstOrDefault()?.Description);
            }

            await unitOfWork.CommitTransactionAsync(cancellationToken);

            await SendConfirmationEmail(newUser, cancellationToken);

            return Result<int>.CreateSuccess(newUser.Id);
        }
        catch (Exception ex)
        {
            Log.Error(ex, ex.Message);

            await unitOfWork.RollbackTransactionAsync(cancellationToken);

            return Result<int>.CreateFailure([new Error(ex.Message)]);
        }
    }

    private async Task<(bool success, Error? error)> ValidateUser(string username, string email, CancellationToken cancellationToken)
    {
        ApplicationUser existingUser = await unitOfWork.IdentityRepository.FindByNameAsync(username, cancellationToken);
        if (existingUser.Id == default)
        {
            existingUser = await unitOfWork.IdentityRepository.FindByEmailAsync(email, cancellationToken);
        }

        if (existingUser.Id != default)
        {
            return (false, new(Constants.Errors.UserAlreadyExists));
        }

        existingUser = await unitOfWork.IdentityRepository.FindByEmailAsync(username, cancellationToken);
        if (existingUser.Id != default)
        {
            return (false, new(Constants.Errors.UsernameMustBeDifferentThanEmail));
        }

        return (true, null);
    }

    private async Task SendConfirmationEmail(ApplicationUser user, CancellationToken cancellationToken)
    {
        string token = await unitOfWork.IdentityRepository.GenerateEmailConfirmationTokenAsync(user, cancellationToken);

        ConfirmUserEmailNotification notification = new(user.Email, user.Id, token);
        await mediator.Publish(notification, cancellationToken);
    }
}
