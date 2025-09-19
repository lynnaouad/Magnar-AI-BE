using Magnar.AI.Application.Dto.Providers;
using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Application.Features.Providers.Commands
{
    public sealed record CreateProviderCommand(ProviderDto Model, int WorkspaceId) : IRequest<Result<int>>;

    public class CreateProviderCommandHandler : IRequestHandler<CreateProviderCommand, Result<int>>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;
        #endregion

        #region Constructor
        public CreateProviderCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
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

            await unitOfWork.ProviderRepository.CreateAsync(provider, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            if(provider.Type == ProviderTypes.API && request.Model?.Details?.ApiProviderDetails is not null && request.Model.Details.ApiProviderDetails.Any())
            {
                await AddApisDetails(request.Model.Details.ApiProviderDetails, request.WorkspaceId, provider.Id, provider.Name, cancellationToken);
            }

            return Result<int>.CreateSuccess(provider.Id);
        }

        #region Private Methods

        public async Task AddApisDetails(IEnumerable<ApiProviderDetailsDto> apis, int workspaceId, int providerId, string providerName, CancellationToken cancellationToken)
        {
            apis = apis.Select(x =>
            {
                x.ProviderId = providerId;
                x.PluginName = $"{providerName}_{workspaceId}_{providerId}";

                return x;
            });

            var mapped = mapper.Map<IEnumerable<ApiProviderDetails>>(apis);

            await unitOfWork.ProviderRepository.ApiProviderDetailsRepository.CreateAsync(mapped, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);
        }

        #endregion
    }
}
