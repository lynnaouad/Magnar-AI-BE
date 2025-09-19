using Magnar.AI.Application.Features.Providers.Commands;
using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Application.Features.Providers.Validators;

public class UpdateProviderCommandValidator : AbstractValidator<UpdateProviderCommand>
{
    public readonly IUnitOfWork unitOfWork;

    public UpdateProviderCommandValidator(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    public override Task<ValidationResult> ValidateAsync(ValidationContext<UpdateProviderCommand> context, CancellationToken cancellation = default)
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

        return base.ValidateAsync(context, cancellation);
    }
}