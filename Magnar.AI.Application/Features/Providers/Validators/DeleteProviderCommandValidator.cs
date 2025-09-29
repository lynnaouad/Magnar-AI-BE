using Magnar.AI.Application.Features.Providers.Commands;
using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Application.Features.Providers.Validators;

public class DeleteProviderCommandValidator : AbstractValidator<DeleteProviderCommand>
{
    public readonly IUnitOfWork unitOfWork;

    public DeleteProviderCommandValidator(IUnitOfWork unitOfWork)
    {
        this.unitOfWork = unitOfWork;
    }

    public override Task<ValidationResult> ValidateAsync(ValidationContext<DeleteProviderCommand> context, CancellationToken cancellation = default)
    {
        RuleFor(x => x.Id)
           .MustAsync(async (id, cancellation) =>
           {
               var exist = await unitOfWork.ProviderRepository.GetProviderAsync(id, cancellation);

               return exist is not null;
           })
          .WithMessage(Constants.Errors.NotFound);

        return base.ValidateAsync(context, cancellation);
    }
}