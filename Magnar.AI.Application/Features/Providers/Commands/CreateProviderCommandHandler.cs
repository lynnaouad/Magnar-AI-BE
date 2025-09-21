using Magnar.AI.Application.Dto.Providers;
using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Application.Features.Providers.Commands
{
    public sealed record CreateProviderCommand(ProviderDto Model, int WorkspaceId) : IRequest<Result<int>>;

    public class CreateProviderCommandHandler : IRequestHandler<CreateProviderCommand, Result<int>>
    {
        #region Members
        private readonly IApiProviderService apiProviderService;
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;
        #endregion

        #region Constructor
        public CreateProviderCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, IApiProviderService apiProviderService)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
            this.apiProviderService = apiProviderService;
        }
        #endregion

        public async Task<Result<int>> Handle(CreateProviderCommand request, CancellationToken cancellationToken)
        {
            if (request.Model.Type == ProviderTypes.SqlServer && request.Model.Details is not null)
            {
                request.Model.Details.SqlServerConfiguration.Password = unitOfWork.ProviderRepository.ProtectPassword(request.Model.Details.SqlServerConfiguration.Password);
            }

            request.Model.WorkspaceId = request.WorkspaceId;

            var provider = mapper.Map<Provider>(request.Model);

            if(provider.Type == ProviderTypes.API && provider.ApiProviderDetails.Any())
            {
                provider.ApiProviderDetails = [.. provider.ApiProviderDetails.Select(x =>
                {
                    x.ProviderId = provider.Id;
                    x.PluginName = $"DynamicPlugin_{provider.WorkspaceId}_{provider.Id}";

                    return x;
                })];
            }

            await unitOfWork.ProviderRepository.CreateAsync(provider, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

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

            apiProviderService.RegisterApis(provider.WorkspaceId, provider.ApiProviderDetails, mapped.Details.ApiProviderAuthDetails);
        }

        #endregion
    }
}
