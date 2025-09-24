using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Application.Features.Providers.Queries
{
    public sealed record HaveAccessOnWorkspaceQuery(int Id, string Username) : IRequest<Result<bool>>;

    public class HaveAccessOnWorkspaceQueryHandler : IRequestHandler<HaveAccessOnWorkspaceQuery, Result<bool>>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        private readonly IAuthorizationService authorizationService;
        #endregion

        #region Constructor
        public HaveAccessOnWorkspaceQueryHandler(IUnitOfWork unitOfWork, IAuthorizationService authorizationService)
        {
            this.unitOfWork = unitOfWork;
            this.authorizationService = authorizationService;
        }
        #endregion

        public async Task<Result<bool>> Handle(HaveAccessOnWorkspaceQuery request, CancellationToken cancellationToken)
        {
            if (request.Id == default)
            {
                return Result<bool>.CreateSuccess(false);
            }

            var canAccessWorkspace = await authorizationService.CanAccessWorkspace(request.Id, cancellationToken);

            return Result<bool>.CreateSuccess(canAccessWorkspace);
        }
    }
}
