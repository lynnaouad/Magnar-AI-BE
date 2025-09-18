using Magnar.AI.Application.Features.Workspaces.Commands;
using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Application.Features.Workspaces.Validators;

public class UpdateWorkspaceCommandValidator : AbstractValidator<UpdateWorkspaceCommand>
{
    public readonly IUnitOfWork unitOfWork;

    public UpdateWorkspaceCommandValidator(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    public override Task<ValidationResult> ValidateAsync(ValidationContext<UpdateWorkspaceCommand> context, CancellationToken cancellation = default)
    {
        RuleFor(x => x.Workspace.Name)
            .NotEmpty()
            .WithMessage(Constants.ValidationMessages.RequiredField);

        return base.ValidateAsync(context, cancellation);
    }
}