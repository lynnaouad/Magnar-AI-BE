using Magnar.AI.Application.Features.ApiKeys.Commands;

namespace Magnar.AI.Application.Features.ApiKeys.Validators
{
    public class UpdateApiKeyCommandValidator : AbstractValidator<UpdateApiKeyCommand>
    {
        public override Task<ValidationResult> ValidateAsync(ValidationContext<UpdateApiKeyCommand> context, CancellationToken cancellation = default)
        {
            RuleFor(x => x.Dto.Name)
                .NotEmpty()
                .NotNull()
                .WithMessage(Constants.ValidationMessages.RequiredField);

            RuleFor(x => x.Dto.TenantId)
              .NotEmpty()
              .NotNull()
              .WithMessage(Constants.ValidationMessages.RequiredField);

            return base.ValidateAsync(context, cancellation);
        }
    }
}
