using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Application.Features.Providers.Commands
{
    public sealed record DeleteProviderCommand(int Id) : IRequest<Result>;

    public class DeleteProviderCommandHandler : IRequestHandler<DeleteProviderCommand, Result>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        private readonly IApiProviderService apiProviderService;
        #endregion

        #region Constructor
        public DeleteProviderCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, IApiProviderService apiProviderService)
        {
            this.unitOfWork = unitOfWork;
            this.apiProviderService = apiProviderService;
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

            RemoveKernelApis(provider.WorkspaceId, provider.ApiProviderDetails.FirstOrDefault()?.PluginName ?? string.Empty, provider.Type);

            return Result.CreateSuccess();
        }

        #region Private Method
        public void RemoveKernelApis(int workspaceId, string pluginName, ProviderTypes providerType)
        {
            if (providerType != ProviderTypes.API)
            {
                return;
            }

            apiProviderService.RemovePlugin(workspaceId, pluginName);
        }
        #endregion
    }
}
