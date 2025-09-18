using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Application.Features.Providers.Queries
{
    public sealed record HaveAccessOnWorkspaceQuery(int Id, string Username) : IRequest<Result<bool>>;

    public class HaveAccessOnWorkspaceQueryHandler : IRequestHandler<HaveAccessOnWorkspaceQuery, Result<bool>>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        #endregion

        #region Constructor
        public HaveAccessOnWorkspaceQueryHandler(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }
        #endregion

        public async Task<Result<bool>> Handle(HaveAccessOnWorkspaceQuery request, CancellationToken cancellationToken)
        {
            if (request.Id == default)
            {
                return Result<bool>.CreateSuccess(false);
            }

            var workspace = await unitOfWork.WorkspaceRepository.FirstOrDefaultAsync(x => x.CreatedBy == request.Username && x.Id == request.Id);

            return Result<bool>.CreateSuccess(workspace is not null);
        }
    }
}
