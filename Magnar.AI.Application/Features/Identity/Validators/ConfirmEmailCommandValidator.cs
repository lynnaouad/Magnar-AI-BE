namespace Magnar.AI.Application.Features.Identity.Validators;

public class ConfirmEmailCommandValidator : AbstractValidator<ConfirmEmailCommand>
{
    public ConfirmEmailCommandValidator()
    {
        RuleFor(x => x.Info.Token)
            .NotEmpty()
            .WithMessage(Constants.ValidationMessages.Identity.InvalidEmailoken);
    }
}
