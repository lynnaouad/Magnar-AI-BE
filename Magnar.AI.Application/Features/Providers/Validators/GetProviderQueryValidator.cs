using Magnar.AI.Application.Features.Providers.Queries;

namespace Magnar.AI.Application.Features.Providers.Validators;

public class GetProviderQueryValidator : AbstractValidator<GetProviderQuery>
{
    public override Task<ValidationResult> ValidateAsync(ValidationContext<GetProviderQuery> context, CancellationToken cancellation = default)
    {
        RuleFor(x => x.Id)
            .NotNull()
            .GreaterThan(0)
          .WithMessage(Constants.Errors.NotFound);

        return base.ValidateAsync(context, cancellation);
    }
}