using Magnar.AI.Application.Interfaces.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace Magnar.AI.Application.Services
{
    public class AuthorizationService : IAuthorizationService
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        private readonly ICurrentUserService currentUserService;
        private readonly IHttpContextAccessor httpContextAccessor;
        #endregion

        #region Constructor
        public AuthorizationService(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IHttpContextAccessor httpContextAccessor)
        {
            this.unitOfWork = unitOfWork;
            this.currentUserService = currentUserService;
            this.httpContextAccessor = httpContextAccessor;
        }
        #endregion

        public async Task<bool> CanAccessWorkspace(int workspaceId, string username, CancellationToken cancellationToken)
        {
            var userWorkspace = httpContextAccessor?.HttpContext?.User.FindFirst("tenant_id")?.Value;
            var authOrigin = httpContextAccessor?.HttpContext?.User.FindFirst("auth_origin")?.Value;

            if (authOrigin == Constants.IdentityApi.Clients.Api.GrantTypes.ApiKey && userWorkspace != workspaceId.ToString())
            {
                return false;
            }

            var canAccessWorkspace = await unitOfWork.WorkspaceRepository.FirstOrDefaultAsync(x => x.CreatedBy == username && x.Id == workspaceId, false, cancellationToken);
            
            return canAccessWorkspace is not null;
        }

        public async Task<bool> CanAccessWorkspace(int workspaceId, CancellationToken cancellationToken)
        {
            var username = currentUserService.GetUsername();

            return await CanAccessWorkspace(workspaceId, username, cancellationToken);
        }
    }
}
