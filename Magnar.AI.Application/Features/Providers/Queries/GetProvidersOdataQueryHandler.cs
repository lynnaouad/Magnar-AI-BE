using Magnar.AI.Application.Dto.Providers;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Models.Responses;
using Microsoft.AspNetCore.OData.Query;

namespace Magnar.AI.Application.Features.Providers.Queries
{
    public sealed record GetProvidersOdataQuery(ODataQueryOptions<Provider> FilterOptions) : IRequest<Result<OdataResponse<ProviderDto>>>;

    public class GetProvidersOdataQueryHandler : IRequestHandler<GetProvidersOdataQuery, Result<OdataResponse<ProviderDto>>>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        #endregion

        #region Constructor
        public GetProvidersOdataQueryHandler(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }
        #endregion

        public async Task<Result<OdataResponse<ProviderDto>>> Handle(GetProvidersOdataQuery request, CancellationToken cancellationToken)
        {
            var result = await unitOfWork.ProviderRepository.GetProvidersOdataAsync(request.FilterOptions, cancellationToken);

            return Result<OdataResponse<ProviderDto>>.CreateSuccess(result);
        }
    }
}
