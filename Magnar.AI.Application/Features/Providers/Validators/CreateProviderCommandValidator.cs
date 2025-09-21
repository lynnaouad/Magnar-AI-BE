using DevExpress.Map.Native;
using Magnar.AI.Application.Features.Providers.Commands;
using Magnar.AI.Application.Interfaces.Infrastructure;
using System.Threading;

namespace Magnar.AI.Application.Features.Providers.Validators;

public class CreateProviderCommandValidator : AbstractValidator<CreateProviderCommand>
{
    public readonly IUnitOfWork unitOfWork;

    public CreateProviderCommandValidator(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    public override Task<ValidationResult> ValidateAsync(ValidationContext<CreateProviderCommand> context, CancellationToken cancellation = default)
    {
        RuleFor(x => x.Model.Name)
            .NotEmpty()
            .WithMessage(Constants.ValidationMessages.RequiredField);

        RuleFor(x => x.Model.Type)
            .NotEmpty()
            .WithMessage(Constants.ValidationMessages.RequiredField);

        RuleFor(x => x.Model)
            .Must(provider =>
            {
                return provider.Type != ProviderTypes.SqlServer ||
                       (provider.Type == ProviderTypes.SqlServer &&
                       provider?.Details?.SqlServerConfiguration is not null &&
                       !string.IsNullOrEmpty(provider?.Details.SqlServerConfiguration.InstanceName) &&
                       !string.IsNullOrEmpty(provider?.Details.SqlServerConfiguration.DatabaseName) &&
                       !string.IsNullOrEmpty(provider?.Details.SqlServerConfiguration.Username) &&
                       !string.IsNullOrEmpty(provider?.Details.SqlServerConfiguration.Password));
            })
            .WithMessage(Constants.ValidationMessages.RequiredField);

        RuleFor(x => x.Model)
            .Must(provider =>
            {
                if (provider.Type != ProviderTypes.API || provider.ApiProviderDetails == null || !provider.ApiProviderDetails.Any())
                {
                    return true;
                }

                if(provider.ApiProviderDetails.Count() != provider.ApiProviderDetails.DistinctBy(x => x.FunctionName).Count())
                {
                    return false;
                }

                return true;
            })
            .WithMessage(Constants.ValidationMessages.FunctionNameExist);

        RuleForEach(x => x.Model.ApiProviderDetails)
            .Must(api =>
            {
                if (string.IsNullOrWhiteSpace(api.FunctionName))
                    return false;

                // only ASCII letters, digits, underscores
                return System.Text.RegularExpressions.Regex.IsMatch(api.FunctionName, "^[a-zA-Z0-9_]+$");
            })
            .WithMessage(Constants.ValidationMessages.FunctionNameFormat)
            .When(x => x.Model.Type == ProviderTypes.API
                    && x.Model.ApiProviderDetails != null
                    && x.Model.ApiProviderDetails.Any());

        return base.ValidateAsync(context, cancellation);
    }
}