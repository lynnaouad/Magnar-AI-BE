using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Application.Features.Providers.Commands
{
    public sealed record DeleteProviderCommand(int Id) : IRequest<Result>;

    public class DeleteProviderCommandHandler : IRequestHandler<DeleteProviderCommand, Result>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        #endregion

        #region Constructor
        public DeleteProviderCommandHandler(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }
        #endregion

        public async Task<Result> Handle(DeleteProviderCommand request, CancellationToken cancellationToken)
        {
            unitOfWork.ProviderRepository.Delete(request.Id);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.CreateSuccess();
        }
    }
}
