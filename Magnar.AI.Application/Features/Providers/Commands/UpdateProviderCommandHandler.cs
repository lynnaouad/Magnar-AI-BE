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
        private readonly IApiProviderService apiProviderService;
        #endregion

        #region Constructor
        public UpdateProviderCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, IApiProviderService apiProviderService)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
            this.apiProviderService = apiProviderService;
        }
        #endregion

        public async Task<Result> Handle(UpdateProviderCommand request, CancellationToken cancellationToken)
        {
            if (request.Model.Type == ProviderTypes.SqlServer && request.Model.Details is not null)
            {
                var protectedPassword = unitOfWork.ProviderRepository.ProtectPassword(request.Model.Details.SqlServerConfiguration.Password);

                request.Model.Details.SqlServerConfiguration.Password = protectedPassword;
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

            unitOfWork.ProviderRepository.Update(provider);

            await unitOfWork.SaveChangesAsync(cancellationToken);

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

            apiProviderService.RegisterApis(provider.WorkspaceId, provider.ApiProviderDetails, mapped.Details.ApiProviderAuthDetails);
        }
        #endregion
    }
}
