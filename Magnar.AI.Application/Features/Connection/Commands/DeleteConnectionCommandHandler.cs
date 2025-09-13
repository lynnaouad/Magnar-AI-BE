using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Application.Features.Connection.Commands
{
    public sealed record DeleteConnectionCommand(int Id) : IRequest<Result>;

    public class DeleteConnectionCommandHandler : IRequestHandler<DeleteConnectionCommand, Result>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        #endregion

        #region Constructor
        public DeleteConnectionCommandHandler(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }
        #endregion

        public async Task<Result> Handle(DeleteConnectionCommand request, CancellationToken cancellationToken)
        {
            unitOfWork.ConnectionRepository.Delete(request.Id);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.CreateSuccess();
        }
    }
}
