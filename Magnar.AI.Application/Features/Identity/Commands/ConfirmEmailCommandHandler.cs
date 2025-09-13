using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Application.Features.Identity.Commands;

public sealed record ConfirmEmailCommand(int UserId, ConfirmEmailDto Info) : IRequest<Result>;

public class ConfirmEmailCommandHandler : IRequestHandler<ConfirmEmailCommand, Result>
{
    private readonly IUnitOfWork unitOfWork;

    public ConfirmEmailCommandHandler(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ConfirmEmailCommand request, CancellationToken cancellationToken)
    {
        ApplicationUser user = await unitOfWork.IdentityRepository.GetUserAsync(request.UserId, cancellationToken);
        if (user.Id == default)
        {
            throw new InvalidOperationException(Constants.Errors.UserNotFound);
        }

        bool validToken = await unitOfWork.IdentityRepository.VerifyUserTokenAsync(user, Constants.TokenTypes.EmailConfirm, request.Info.Token, cancellationToken);
        if (!validToken)
        {
            return Result.CreateFailure([new(Constants.Errors.InvalidEmailConfirmToken)]);
        }

        return (await unitOfWork.IdentityRepository.ConfirmEmailAsync(user, request.Info.Token, cancellationToken))
            .ToApplicationResult();
    }
}
