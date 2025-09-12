namespace Magnar.AI.Application.Features.Identity.Validators;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.ResetPasswordDto.ResetToken)
            .NotEmpty()
            .WithMessage(Constants.ValidationMessages.Identity.InvalidResetToken);

        RuleFor(x => x.ResetPasswordDto.NewPassword)
            .NotEmpty()
            .WithMessage(Constants.ValidationMessages.Identity.InvalidNewPassword);
    }
}
