using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Application.Features.ApiKeys.Commands
{
    public sealed record RevokeApiKeyCommand(int Id) : IRequest<Result<bool>>;

    public class RevokeApiKeyCommandHandler : IRequestHandler<RevokeApiKeyCommand, Result<bool>>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        private readonly ICurrentUserService currentUserService;
        #endregion

        #region Constructor

       public RevokeApiKeyCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            this.unitOfWork = unitOfWork;
            this.currentUserService = currentUserService;
        }
        #endregion

        public async Task<Result<bool>> Handle(RevokeApiKeyCommand request, CancellationToken cancellationToken)
        {
            var apiKey = await unitOfWork.ApiKeyRepository.GetAsync(request.Id, false, cancellationToken);
            if(apiKey is null)
            {
                return Result<bool>.CreateFailure([new(Constants.Errors.NotFound)]);
            }

            if(apiKey.OwnerUserId != currentUserService.GetId())
            {
                return Result<bool>.CreateFailure([new(Constants.Errors.Unauthorized)]);
            }

            var result = await unitOfWork.ApiKeyRepository.RevokeAsync(apiKey.PublicId, currentUserService.GetId(), apiKey.TenantId, cancellationToken);

            return Result<bool>.CreateSuccess(result);
        }
    }
}