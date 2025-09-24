using Magnar.AI.Application.Dto.Providers;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Microsoft.AspNetCore.Http;

namespace Magnar.AI.Application.Features.Providers.Queries
{
    public sealed record GetProviderQuery(int Id) : IRequest<Result<ProviderDto>>;

    public class GetProviderQueryHandler : IRequestHandler<GetProviderQuery, Result<ProviderDto>>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        private readonly ICurrentUserService currentUserService;
        private readonly IAuthorizationService authorizationService;
        #endregion

        #region Constructor
        public GetProviderQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService, IAuthorizationService authorizationService)
        {
            this.unitOfWork = unitOfWork;
            this.currentUserService = currentUserService;
            this.authorizationService = authorizationService;
        }
        #endregion

        public async Task<Result<ProviderDto>> Handle(GetProviderQuery request, CancellationToken cancellationToken)
        {
            var provider = await unitOfWork.ProviderRepository.GetProviderAsync(request.Id, cancellationToken);
            if (provider is null)
            {
                return Result<ProviderDto>.CreateFailure([new(Constants.Errors.NotFound)]);
            }

            var canAccessWorkspace = await authorizationService.CanAccessWorkspace(provider.WorkspaceId, cancellationToken);
            if (!canAccessWorkspace)
            {
                return Result<ProviderDto>.CreateFailure([new(Constants.Errors.Unauthorized)], StatusCodes.Status401Unauthorized);
            }

            return Result<ProviderDto>.CreateSuccess(provider);
        }
    }
}
