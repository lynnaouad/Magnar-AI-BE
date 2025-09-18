using Magnar.AI.Application.Dto.Workspaces;
using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Application.Features.Workspaces.Commands
{
    public sealed record UpdateWorkspaceCommand(WorkspaceDto Workspace) : IRequest<Result>;

    public class UpdateWorkspaceCommandHandler : IRequestHandler<UpdateWorkspaceCommand, Result>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;
        #endregion

        #region Constructor
        public UpdateWorkspaceCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
        }
        #endregion

        public async Task<Result> Handle(UpdateWorkspaceCommand request, CancellationToken cancellationToken)
        {
            var workspace = mapper.Map<Workspace>(request.Workspace);

            unitOfWork.WorkspaceRepository.Update(workspace);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.CreateSuccess();
        }
    }
}
