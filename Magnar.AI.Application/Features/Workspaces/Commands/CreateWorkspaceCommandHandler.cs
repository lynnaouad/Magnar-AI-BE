using Magnar.AI.Application.Dto.Workspaces;
using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Application.Features.Workspaces.Commands
{
    public sealed record CreateWorkspaceCommand(WorkspaceDto Workspace) : IRequest<Result>;

    public class CreateWorkspaceCommandHandler : IRequestHandler<CreateWorkspaceCommand, Result>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;
        #endregion

        #region Constructor
        public CreateWorkspaceCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
        }
        #endregion

        public async Task<Result> Handle(CreateWorkspaceCommand request, CancellationToken cancellationToken)
        {
            var workspace = mapper.Map<Workspace>(request.Workspace);

            await unitOfWork.WorkspaceRepository.CreateAsync(workspace, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.CreateSuccess();
        }
    }
}
