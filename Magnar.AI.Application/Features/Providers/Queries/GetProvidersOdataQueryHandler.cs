using Magnar.AI.Application.Dto.Providers;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Models.Responses;
using Microsoft.AspNetCore.OData.Query;

namespace Magnar.AI.Application.Features.Providers.Queries
{
    public sealed record GetProvidersOdataQuery(int WorkspaceId, ODataQueryOptions<Provider> FilterOptions) : IRequest<Result<OdataResponse<ProviderDto>>>;

    public class GetProvidersOdataQueryHandler : IRequestHandler<GetProvidersOdataQuery, Result<OdataResponse<ProviderDto>>>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;
        #endregion

        #region Constructor
        public GetProvidersOdataQueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
        }
        #endregion

        public async Task<Result<OdataResponse<ProviderDto>>> Handle(GetProvidersOdataQuery request, CancellationToken cancellationToken)
        {
            var odataResult = await unitOfWork.ProviderRepository.OdataGetAsync(request.FilterOptions,x => x.WorkspaceId == request.WorkspaceId, cancellationToken: cancellationToken);

            var mappedProviders = mapper.Map<IEnumerable<ProviderDto>>(odataResult.Value);

            var result = new OdataResponse<ProviderDto>
            {
                TotalCount = odataResult.TotalCount,
                Value = mappedProviders
            };

            return Result<OdataResponse<ProviderDto>>.CreateSuccess(result);
        }
    }
}
