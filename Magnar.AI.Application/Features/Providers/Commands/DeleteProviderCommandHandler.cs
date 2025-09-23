using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Domain.Entities;

namespace Magnar.AI.Application.Features.Providers.Commands
{
    public sealed record DeleteProviderCommand(int Id) : IRequest<Result>;

    public class DeleteProviderCommandHandler : IRequestHandler<DeleteProviderCommand, Result>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        private readonly IKernelPluginService kernelPluginService;
        #endregion

        #region Constructor
        public DeleteProviderCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, IKernelPluginService kernelPluginService)
        {
            this.unitOfWork = unitOfWork;
            this.kernelPluginService = kernelPluginService;
        }
        #endregion

        public async Task<Result> Handle(DeleteProviderCommand request, CancellationToken cancellationToken)
        {
            var provider = await unitOfWork.ProviderRepository.GetProviderAsync(request.Id, cancellationToken);
            if (provider is null)
            {
                return Result.CreateFailure([new(Constants.Errors.NotFound)]);
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
