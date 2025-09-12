using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Features.Identity.Notifications;

namespace Magnar.AI.Application.Features.Identity.Commands;

public sealed record RecoverPasswordCommand(string Email) : IRequest<Result>;

public class RecoverPasswordCommandHandler : IRequestHandler<RecoverPasswordCommand, Result>
{
    private readonly IMediator mediator;
    private readonly IUnitOfWork unitOfWork;

    public RecoverPasswordCommandHandler(
        IUnitOfWork unitOfWork,
        IMediator mediator)
    {
        this.mediator = mediator;
        this.unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RecoverPasswordCommand request, CancellationToken cancellationToken)
    {
        ApplicationUser user = await unitOfWork.IdentityRepository.FindByNameOrEmailAsync(request.Email, cancellationToken);
        if (string.IsNullOrEmpty(user.Id))
        {
            return Result.CreateFailure([new(Constants.Errors.UserNotFound)]);
        }

        string token = await unitOfWork.IdentityRepository.GeneratePasswordResetTokenAsync(user, cancellationToken);

        ResetUserPasswordNotification notification = new(user.Email, user.Id, token);
        await mediator.Publish(notification, cancellationToken);

        return Result.CreateSuccess();
    }
}
