using Magnar.AI.Application.Dto.Providers;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Interfaces.Services;
using Magnar.AI.Domain.Entities;
using Microsoft.AspNetCore.Http;
using System.Threading;

namespace Magnar.AI.Application.Features.Providers.Commands
{
    public sealed record CreateProviderCommand(ProviderDto Model) : IRequest<Result<int>>;

    public class CreateProviderCommandHandler : IRequestHandler<CreateProviderCommand, Result<int>>
    {
        #region Members
        private readonly IKernelPluginService kernelPluginService;
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;
        private readonly ICurrentUserService currentUserService;
        private readonly IAuthorizationService authorizationService;
        #endregion

        #region Constructor
        public CreateProviderCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, IKernelPluginService kernelPluginService, ICurrentUserService currentUserService, IAuthorizationService authorizationService)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
            this.kernelPluginService = kernelPluginService;
            this.currentUserService = currentUserService;
            this.authorizationService = authorizationService;
        }
        #endregion

        public async Task<Result<int>> Handle(CreateProviderCommand request, CancellationToken cancellationToken)
        {
            var canAccessWorkspace = await authorizationService.CanAccessWorkspace(request.Model.WorkspaceId, cancellationToken);
            if (!canAccessWorkspace)
            {
                return Result<int>.CreateFailure([new(Constants.Errors.Unauthorized)], StatusCodes.Status401Unauthorized);
            }

            var provider = mapper.Map<Provider>(request.Model);

            if (provider.Type == ProviderTypes.API && provider.ApiProviderDetails.Count != 0)
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

            await unitOfWork.ProviderRepository.CreateAsync(provider, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            await unitOfWork.CommitTransactionAsync(cancellationToken);

            RegisterKernelApis(provider);

            return Result<int>.CreateSuccess(provider.Id);
        }

        #region Private Methods

        public void RegisterKernelApis(Provider provider)
        {
            if(provider.Type != ProviderTypes.API || provider.ApiProviderDetails is null)
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
                            && x.IsDefault, false, cancellationToken);

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
