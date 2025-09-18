using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Application.Features.Workspaces.Commands
{
    public sealed record DeleteWorkspaceCommand(int Id) : IRequest<Result>;

    public class DeleteWorkspaceCommandHandler : IRequestHandler<DeleteWorkspaceCommand, Result>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        #endregion

        #region Constructor
        public DeleteWorkspaceCommandHandler(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }
        #endregion

        public async Task<Result> Handle(DeleteWorkspaceCommand request, CancellationToken cancellationToken)
        {
            unitOfWork.WorkspaceRepository.Delete(request.Id);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.CreateSuccess();
        }
    }
}
