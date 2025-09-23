using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Application.Features.Providers.Commands
{
    public sealed record DeleteProviderCommand(int Id) : IRequest<Result>;

    public class DeleteProviderCommandHandler : IRequestHandler<DeleteProviderCommand, Result>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        private readonly ICurrentUserService currentUserService;
        private readonly IKernelPluginService kernelPluginService;
        #endregion

        #region Constructor
        public DeleteProviderCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, IKernelPluginService kernelPluginService, ICurrentUserService currentUserService)
        {
            this.unitOfWork = unitOfWork;
            this.kernelPluginService = kernelPluginService;
            this.currentUserService = currentUserService;
        }
        #endregion

        public async Task<Result> Handle(DeleteProviderCommand request, CancellationToken cancellationToken)
        {
            var provider = await unitOfWork.ProviderRepository.GetProviderAsync(request.Id, cancellationToken);
            if (provider is null)
            {
                return Result.CreateFailure([new(Constants.Errors.NotFound)]);
            }

            var canAccessWorkspace = await unitOfWork.WorkspaceRepository.FirstOrDefaultAsync(x => x.CreatedBy == currentUserService.GetUsername() && x.Id == provider.WorkspaceId, false, cancellationToken);
            if (canAccessWorkspace is null)
            {
                return Result<int>.CreateFailure([new(Constants.Errors.Unauthorized)]);
            }

            unitOfWork.ProviderRepository.Delete(request.Id);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            RemoveKernelApis(provider.WorkspaceId, provider.ApiProviderDetails.FirstOrDefault()?.PluginName ?? string.Empty, provider.Type, provider.Id);

            return Result.CreateSuccess();
        }

        #region Private Method
        public void RemoveKernelApis(int workspaceId, string pluginName, ProviderTypes providerType, int providerId)
        {
            if (providerType != ProviderTypes.API)
            {
                return;
            }

            kernelPluginService.RemoveApiPlugin(workspaceId, providerId, pluginName);
        }
        #endregion
    }
}
