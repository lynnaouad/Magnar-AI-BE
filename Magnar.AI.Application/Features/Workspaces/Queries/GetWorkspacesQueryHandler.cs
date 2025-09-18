using Magnar.AI.Application.Dto.Workspaces;
using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Application.Features.Providers.Queries
{
    public sealed record GetWorkspacesQuery(string Username) : IRequest<Result<IEnumerable<WorkspaceDto>>>;

    public class GetWorkspacesQueryHandler : IRequestHandler<GetWorkspacesQuery, Result<IEnumerable<WorkspaceDto>>>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;
        #endregion

        #region Constructor
        public GetWorkspacesQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
        }
        #endregion

        public async Task<Result<IEnumerable<WorkspaceDto>>> Handle(GetWorkspacesQuery request, CancellationToken cancellationToken)
        {
            var workspaces = await unitOfWork.WorkspaceRepository.WhereAsync(x => x.CreatedBy == request.Username);

            return Result<IEnumerable<WorkspaceDto>>.CreateSuccess(mapper.Map<IEnumerable<WorkspaceDto>>(workspaces));
        }
    }
}
