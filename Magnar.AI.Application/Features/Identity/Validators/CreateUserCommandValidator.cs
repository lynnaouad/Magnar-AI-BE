namespace Magnar.AI.Application.Features.Identity.Validators;

public class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
{
    public CreateUserCommandValidator()
    {
        RuleFor(x => x.Info.ApplicationUserDto.Username)
           .NotEmpty()
           .WithMessage(Constants.ValidationMessages.Identity.UsernameRequired);

        RuleFor(x => x.Info.ApplicationUserDto.FirstName)
          .NotEmpty()
          .WithMessage(Constants.ValidationMessages.Identity.FirstNameRequired);

        RuleFor(x => x.Info.ApplicationUserDto.LastName)
          .NotEmpty()
          .WithMessage(Constants.ValidationMessages.Identity.LastNameRequired);

        RuleFor(x => x.Info.ApplicationUserDto.Email)
            .EmailAddress()
            .WithMessage(Constants.ValidationMessages.Identity.InvalidEmail);

        RuleFor(x => x.Info.ApplicationUserDto.Password)
            .NotEmpty()
            .WithMessage(Constants.ValidationMessages.Identity.PasswordRequired);
    }
}
