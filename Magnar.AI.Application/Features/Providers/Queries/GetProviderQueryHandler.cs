using Magnar.AI.Application.Dto.Providers;
using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Application.Features.Providers.Queries
{
    public sealed record GetProviderQuery(int Id) : IRequest<Result<ProviderDto>>;

    public class GetProviderQueryHandler : IRequestHandler<GetProviderQuery, Result<ProviderDto>>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        private readonly ICurrentUserService currentUserService;
        #endregion

        #region Constructor
        public GetProviderQueryHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            this.unitOfWork = unitOfWork;
            this.currentUserService = currentUserService;
        }
        #endregion

        public async Task<Result<ProviderDto>> Handle(GetProviderQuery request, CancellationToken cancellationToken)
        {
            var provider = await unitOfWork.ProviderRepository.GetProviderAsync(request.Id, cancellationToken);
            if (provider is null)
            {
                return Result<ProviderDto>.CreateFailure([new(Constants.Errors.NotFound)]);
            }

            var canAccessWorkspace = await unitOfWork.WorkspaceRepository.FirstOrDefaultAsync(x => x.CreatedBy == currentUserService.GetUsername() && x.Id == provider.WorkspaceId, false, cancellationToken);
            if (canAccessWorkspace is null)
            {
                return Result<ProviderDto>.CreateFailure([new(Constants.Errors.Unauthorized)]);
            }

            return Result<ProviderDto>.CreateSuccess(provider);
        }
    }
}
