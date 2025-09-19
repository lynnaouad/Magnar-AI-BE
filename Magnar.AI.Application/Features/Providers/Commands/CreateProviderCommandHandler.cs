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

            var provider = mapper.Map<Provider>(request.Model);

            provider.WorkspaceId = request.WorkspaceId;

            await unitOfWork.ProviderRepository.CreateAsync(provider, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result<int>.CreateSuccess(provider.Id);
        }
    }
}
