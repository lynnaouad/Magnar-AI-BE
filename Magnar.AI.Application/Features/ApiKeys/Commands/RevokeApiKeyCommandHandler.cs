using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Application.Features.ApiKeys.Commands
{
    public sealed record RevokeApiKeyCommand(string PublicId) : IRequest<Result<bool>>;

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
            var result = await unitOfWork.ApiKeyRepository.RevokeAsync(request.PublicId, currentUserService.GetId(), string.Empty, cancellationToken);

            return Result<bool>.CreateSuccess(result);
        }
    }
}