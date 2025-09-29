using Magnar.AI.Application.Dto.Providers;
using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Application.Features.Providers.Queries
{
    public sealed record GetProvidersQuery(ProviderFilterDto Filters) : IRequest<Result<IEnumerable<ProviderDto>>>;

    public class GetProvidersQueryHandler : IRequestHandler<GetProvidersQuery, Result<IEnumerable<ProviderDto>>>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;
        #endregion

        #region Constructor
        public GetProvidersQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
        }
        #endregion

        public async Task<Result<IEnumerable<ProviderDto>>> Handle(GetProvidersQuery request, CancellationToken cancellationToken)
        {
            var result = await unitOfWork.ProviderRepository.GetProvidersAsync(x => 
                x.WorkspaceId == request.Filters.WorkspaceId &&
                (
                    request.Filters.ProviderType == null ||
                    x.Type == request.Filters.ProviderType
                )
                , cancellationToken);

            var mappedProviders = mapper.Map<IEnumerable<ProviderDto>>(result);

            return Result<IEnumerable<ProviderDto>>.CreateSuccess(mappedProviders);
        }
    }
}
