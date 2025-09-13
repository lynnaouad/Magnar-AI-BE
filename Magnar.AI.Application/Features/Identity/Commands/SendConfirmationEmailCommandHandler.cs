using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Features.Identity.Notifications;

namespace Magnar.AI.Application.Features.Identity.Commands;

public sealed record SendConfirmationEmailCommand(int UserId) : IRequest<Result>;

public class SendConfirmationEmailCommandHandler : IRequestHandler<SendConfirmationEmailCommand, Result>
{
    private readonly IMediator mediator;
    private readonly IUnitOfWork unitOfWork;

    public SendConfirmationEmailCommandHandler(
        IMediator mediator,
        IUnitOfWork unitOfWork)
    {
        this.mediator = mediator;
        this.unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SendConfirmationEmailCommand request, CancellationToken cancellationToken)
    {
        ApplicationUser user = await unitOfWork.IdentityRepository.GetUserAsync(request.UserId, cancellationToken);
        if (user.Id == 0)
        {
            throw new InvalidOperationException(Constants.Errors.UserNotFound);
        }

        if (user.Email is null)
        {
            return Result.CreateFailure([new(Constants.Errors.InvalidEmail)]);
        }

        string token = await unitOfWork.IdentityRepository.GenerateEmailConfirmationTokenAsync(user, cancellationToken);

        ConfirmUserEmailNotification notification = new(user.Email, user.Id, token);
        await mediator.Publish(notification, cancellationToken);

        return Result.CreateSuccess();
    }
}
