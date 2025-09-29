using Magnar.AI.Application.Dto.ApiKeys;
using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Application.Features.ApiKeys.Commands
{
    public sealed record UpdateApiKeyCommand(ApiKeyDto Dto) : IRequest<Result>;

    public class UpdateApiKeyCommandHandler : IRequestHandler<UpdateApiKeyCommand, Result>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        private readonly ICurrentUserService currentUserService;
        #endregion

        #region Constructor

        public UpdateApiKeyCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            this.unitOfWork = unitOfWork;
            this.currentUserService = currentUserService;
        }
        #endregion

        public async Task<Result> Handle(UpdateApiKeyCommand request, CancellationToken cancellationToken)
        {
            var oldKey = await unitOfWork.ApiKeyRepository.FirstOrDefaultAsync(k => k.PublicId == request.Dto.PublicId && k.OwnerUserId == currentUserService.GetId() && k.TenantId == request.Dto.TenantId, false, cancellationToken: cancellationToken);
            if(oldKey is null)
            {
                return Result.CreateFailure([new(Constants.Errors.NotFound)]);
            }

            oldKey.Name = request.Dto.Name;

            unitOfWork.ApiKeyRepository.Update(oldKey);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.CreateSuccess();
        }
    }
}