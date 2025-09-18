using Magnar.AI.Application.Features.Workspaces.Commands;

namespace Magnar.AI.Application.Features.Workspaces.Validators;

public class CreateWorkspaceCommandValidator : AbstractValidator<CreateWorkspaceCommand>
{
    public override Task<ValidationResult> ValidateAsync(ValidationContext<CreateWorkspaceCommand> context, CancellationToken cancellation = default)
    {
        RuleFor(x => x.Workspace.Name)
            .NotEmpty()
            .WithMessage(Constants.ValidationMessages.RequiredField);

        return base.ValidateAsync(context, cancellation);
    }
}