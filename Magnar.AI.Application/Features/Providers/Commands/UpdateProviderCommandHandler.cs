using Magnar.AI.Application.Dto.Providers;
using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Application.Features.Providers.Commands
{
    public sealed record UpdateProviderCommand(ProviderDto Model) : IRequest<Result>;

    public class UpdateProviderCommandHandler : IRequestHandler<UpdateProviderCommand, Result>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;
        private readonly ICurrentUserService currentUserService;
        private readonly IKernelPluginService kernelPluginService;
        #endregion

        #region Constructor
        public UpdateProviderCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, IKernelPluginService kernelPluginService, ICurrentUserService currentUserService)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
            this.kernelPluginService = kernelPluginService;
            this.currentUserService = currentUserService;
        }
        #endregion

        public async Task<Result> Handle(UpdateProviderCommand request, CancellationToken cancellationToken)
        {
            var canAccessWorkspace = await unitOfWork.WorkspaceRepository.FirstOrDefaultAsync(x => x.CreatedBy == currentUserService.GetUsername() && x.Id == request.Model.WorkspaceId, false, cancellationToken);
            if (canAccessWorkspace is null)
            {
                return Result<int>.CreateFailure([new(Constants.Errors.Unauthorized)]);
            }

            var provider = mapper.Map<Provider>(request.Model);

            if (provider.Type == ProviderTypes.API && provider.ApiProviderDetails.Any())
            {
                provider.ApiProviderDetails = [.. provider.ApiProviderDetails.Select(x =>
                {
                    x.ProviderId = provider.Id;
                    x.PluginName = $"DynamicPlugin_{provider.WorkspaceId}_{provider.Id}";

                    return x;
                })];
            }

            await unitOfWork.BeginTransactionAsync(cancellationToken);

            await RemoveOldDefaultConnection(provider, cancellationToken);

            unitOfWork.ProviderRepository.Update(provider);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            await unitOfWork.CommitTransactionAsync(cancellationToken);

            UpdateRegisteredKernelApis(provider);

            return Result.CreateSuccess();
        }

        #region Private Methods

        private void UpdateRegisteredKernelApis(Provider provider)
        {
            if (provider.Type != ProviderTypes.API || provider.ApiProviderDetails is null)
            {
                return;
            }

            var mapped = mapper.Map<ProviderDto>(provider);
            if (mapped.Details?.ApiProviderAuthDetails is null)
            {
                return;
            }

            kernelPluginService.RegisterApiFunctions(provider.WorkspaceId, provider.Id, provider.ApiProviderDetails, mapped.Details.ApiProviderAuthDetails);
        }

        public async Task RemoveOldDefaultConnection(Provider provider, CancellationToken cancellationToken)
        {
            if (!provider.IsDefault)
            {
                return;
            }

            var existingDefaults = await unitOfWork.ProviderRepository.WhereAsync(x => x.Type == provider.Type
                            && x.WorkspaceId == provider.WorkspaceId
                            && x.IsDefault
                            && x.Id != provider.Id, false, cancellationToken);

            if (existingDefaults.Any())
            {
                existingDefaults = [.. existingDefaults.Select(x =>
                    {
                        x.IsDefault = false;
                        return x;
                    })];

                unitOfWork.ProviderRepository.Update(existingDefaults);
                await unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }
        #endregion
    }
}
