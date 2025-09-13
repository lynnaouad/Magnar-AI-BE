using Magnar.AI.Application.Features.Connection.Queries;

namespace Magnar.AI.Application.Features.Connection.Validators;

public class GetConnectionQueryValidator : AbstractValidator<GetConnectionQuery>
{
    public override Task<ValidationResult> ValidateAsync(ValidationContext<GetConnectionQuery> context, CancellationToken cancellation = default)
    {
        RuleFor(x => x.Id)
            .NotNull()
            .GreaterThan(0)
          .WithMessage(Constants.Errors.NotFound);

        return base.ValidateAsync(context, cancellation);
    }
}