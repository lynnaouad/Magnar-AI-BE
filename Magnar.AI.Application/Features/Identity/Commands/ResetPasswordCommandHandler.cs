using Magnar.AI.Application.Extensions;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Serilog;

namespace Magnar.AI.Application.Features.Identity.Commands;

public sealed record ResetPasswordCommand(int UserId, ResetPasswordDto ResetPasswordDto) : IRequest<Result>;

public class ResetPasswordCommandHandler : IRequestHandler<ResetPasswordCommand, Result>
{
    private readonly IUnitOfWork unitOfWork;

    public ResetPasswordCommandHandler(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        ApplicationUser user = await unitOfWork.IdentityRepository.GetUserAsync(request.UserId, cancellationToken);
        if (string.IsNullOrEmpty(user.Id))
        {
            throw new InvalidOperationException(Constants.Errors.UserNotFound);
        }

        bool validToken = await unitOfWork.IdentityRepository.VerifyUserTokenAsync(user, Constants.TokenTypes.PasswordReset, request.ResetPasswordDto.ResetToken, cancellationToken);
        if (!validToken)
        {
            return Result.CreateFailure([new(Constants.Errors.InvalidResetPasswordToken)]);
        }

        try
        {
            await unitOfWork.BeginTransactionAsync(cancellationToken);

            IdentityResult result = await unitOfWork.IdentityRepository.ResetPasswordAsync(user, request.ResetPasswordDto.ResetToken, request.ResetPasswordDto.NewPassword, cancellationToken);
            if (!result.Succeeded)
            {
                return result.ToApplicationResult();
            }

            // If the email has not been confirmed yet, it will be automatically confirmed during the password reset process.
            if (!await unitOfWork.IdentityRepository.EmailConfirmedAsync(user, cancellationToken))
            {
                string emailConfirmationToken = await unitOfWork.IdentityRepository.GenerateEmailConfirmationTokenAsync(user, cancellationToken);

                return (await unitOfWork.IdentityRepository.ConfirmEmailAsync(user, emailConfirmationToken, cancellationToken)).ToApplicationResult();
            }

            await unitOfWork.CommitTransactionAsync(cancellationToken);

            return Result.CreateSuccess();
        }
        catch (Exception ex)
        {
            Log.Error(ex, ex.Message);

            await unitOfWork.RollbackTransactionAsync(cancellationToken);

            return Result.CreateFailure([new(Constants.Errors.CannotResetPassword)]);
        }
    }
}
