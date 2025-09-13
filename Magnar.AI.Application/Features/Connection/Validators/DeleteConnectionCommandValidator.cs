using Magnar.AI.Application.Features.Connection.Commands;
using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Application.Features.Connection.Validators;

public class DeleteConnectionCommandValidator : AbstractValidator<DeleteConnectionCommand>
{
    public readonly IUnitOfWork unitOfWork;

    public DeleteConnectionCommandValidator(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    public override Task<ValidationResult> ValidateAsync(ValidationContext<DeleteConnectionCommand> context, CancellationToken cancellation = default)
    {
        RuleFor(x => x.Id)
           .MustAsync(async (id, cancellation) =>
           {
               var exist = await unitOfWork.ConnectionRepository.GetAsync(id, false, cancellation);

               return exist is not null;
           })
          .WithMessage(Constants.Errors.NotFound);

        return base.ValidateAsync(context, cancellation);
    }
}