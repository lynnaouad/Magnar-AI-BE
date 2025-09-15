using Magnar.AI.Application.Features.Connection.Commands;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Domain.Static;

namespace Magnar.AI.Application.Features.Connection.Validators;

public class CreateConnectionCommandValidator : AbstractValidator<CreateConnectionCommand>
{
    public readonly IUnitOfWork unitOfWork;

    public CreateConnectionCommandValidator(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    public override Task<ValidationResult> ValidateAsync(ValidationContext<CreateConnectionCommand> context, CancellationToken cancellation = default)
    {
        RuleFor(x => x.Connection)
           .MustAsync(async (connection, cancellation) =>
           {
               var defaultConnectionExist = await unitOfWork.ConnectionRepository.FirstOrDefaultAsync(x => x.IsDefault, false, cancellation);

               return !connection.IsDefault || defaultConnectionExist is null;
           })
          .WithMessage(Constants.ValidationMessages.CannotHaveMultipleDefaultConnections);

        RuleFor(x => x.Connection.Name)
            .NotEmpty()
            .WithMessage(Constants.ValidationMessages.RequiredField);

        RuleFor(x => x.Connection.Provider)
            .NotEmpty()
            .WithMessage(Constants.ValidationMessages.RequiredField);

        RuleFor(x => x.Connection)
            .Must(connection =>
            {
                return connection.Provider != ProviderTypes.SqlServer ||
                       (connection.Provider == ProviderTypes.SqlServer &&
                       connection?.Details?.SqlServerConfiguration is not null &&
                       !string.IsNullOrEmpty(connection?.Details.SqlServerConfiguration.InstanceName) &&
                       !string.IsNullOrEmpty(connection?.Details.SqlServerConfiguration.DatabaseName) &&
                       !string.IsNullOrEmpty(connection?.Details.SqlServerConfiguration.Username) &&
                       !string.IsNullOrEmpty(connection?.Details.SqlServerConfiguration.Password));
            })
            .WithMessage(Constants.ValidationMessages.RequiredField);

        return base.ValidateAsync(context, cancellation);
    }
}