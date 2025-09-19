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
        #endregion

        #region Constructor
        public UpdateProviderCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
        }
        #endregion

        public async Task<Result> Handle(UpdateProviderCommand request, CancellationToken cancellationToken)
        {
            if (request.Model.Type == ProviderTypes.SqlServer && request.Model.Details is not null)
            {
                var protectedPassword = unitOfWork.ProviderRepository.ProtectPassword(request.Model.Details.SqlServerConfiguration.Password);

                request.Model.Details.SqlServerConfiguration.Password = protectedPassword;
            }

            unitOfWork.ProviderRepository.Update(mapper.Map<Provider>(request.Model));

            if (request.Model.Type == ProviderTypes.API &&
                request.Model?.Details?.ApiProviderDetails is not null)
            {
                await UpdateApisDetails(request.Model.Details.ApiProviderDetails, request.Model.WorkspaceId, request.Model.Id, request.Model.Name, cancellationToken);
            }

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.CreateSuccess();
        }

        #region Private Methods

        private async Task UpdateApisDetails(IEnumerable<ApiProviderDetailsDto> apis, int workspaceId, int providerId, string providerName, CancellationToken cancellationToken)
        {
            apis = apis.Select(x =>
            {
                x.ProviderId = providerId;
                x.PluginName = $"{providerName}_{workspaceId}_{providerId}";
                return x;
            });

            var mapped = mapper.Map<IEnumerable<ApiProviderDetails>>(apis);

            await unitOfWork.ProviderRepository.DeleteApiDetailsAsync(providerId, cancellationToken);
            await unitOfWork.ProviderRepository.ApiProviderDetailsRepository.CreateAsync(mapped, cancellationToken);
        }

        #endregion
    }
}
