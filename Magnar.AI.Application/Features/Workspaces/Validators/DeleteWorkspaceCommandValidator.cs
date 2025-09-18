using Magnar.AI.Application.Features.Workspaces.Commands;
using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Application.Features.Workspaces.Validators;

public class DeleteWorkspaceCommandValidator : AbstractValidator<DeleteWorkspaceCommand>
{
    public readonly IUnitOfWork unitOfWork;

    public DeleteWorkspaceCommandValidator(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    public override Task<ValidationResult> ValidateAsync(ValidationContext<DeleteWorkspaceCommand> context, CancellationToken cancellation = default)
    {
        RuleFor(x => x.Id)
           .MustAsync(async (id, cancellation) =>
           {
               var exist = await unitOfWork.WorkspaceRepository.GetAsync(id, false, cancellation);

               return exist is not null;
           })
          .WithMessage(Constants.Errors.NotFound);

        return base.ValidateAsync(context, cancellation);
    }
}