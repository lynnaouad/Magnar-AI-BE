using Magnar.AI.Application.Features.DatabaseSchema.Queries;

namespace Magnar.AI.Application.Features.DatabaseSchema.Validators
{
    public class GetTablesQueryValidator : AbstractValidator<GetTablesQuery>
    {
        public override Task<ValidationResult> ValidateAsync(ValidationContext<GetTablesQuery> context, CancellationToken cancellation = default)
        {
            RuleFor(x => x.Provider)
                .Must(x => 
                { return 
                    x.Type == ProviderTypes.SqlServer && 
                    x?.Details?.SqlServerConfiguration != null &&
                    !string.IsNullOrEmpty(x.Details.SqlServerConfiguration.Username) &&
                    !string.IsNullOrEmpty(x.Details.SqlServerConfiguration.Password) &&
                    !string.IsNullOrEmpty(x.Details.SqlServerConfiguration.InstanceName) &&
                    !string.IsNullOrEmpty(x.Details.SqlServerConfiguration.DatabaseName);            
                })
                .WithMessage(Constants.ValidationMessages.RequiredField);

            return base.ValidateAsync(context, cancellation);
        }
    }
}
