namespace Magnar.AI.Application.Features.Identity.Validators;

public class RecoverPasswordCommandValidator : AbstractValidator<RecoverPasswordCommand>
{
    public RecoverPasswordCommandValidator()
    {
        RuleFor(x => x.Email)
            .EmailAddress()
            .WithMessage(Constants.ValidationMessages.Identity.InvalidEmail);
    }
}
