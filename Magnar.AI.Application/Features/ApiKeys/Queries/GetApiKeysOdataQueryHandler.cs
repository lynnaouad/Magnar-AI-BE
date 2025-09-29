using Magnar.AI.Application.Dto.ApiKeys;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Models.Responses;
using Microsoft.AspNetCore.OData.Query;

namespace Magnar.AI.Application.Features.ApiKeys.Queries
{
    public sealed record GetApiKeysOdataQuery(ODataQueryOptions<ApiKey> FilterOptions) : IRequest<Result<OdataResponse<ApiKeyDto>>>;

    public class GetApiKeysOdataQueryHandler : IRequestHandler<GetApiKeysOdataQuery, Result<OdataResponse<ApiKeyDto>>>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;
        private readonly ICurrentUserService currentUserService;
        #endregion

        #region Constructor

        public GetApiKeysOdataQueryHandler(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
            this.currentUserService = currentUserService;
        }
        #endregion

        public async Task<Result<OdataResponse<ApiKeyDto>>> Handle(GetApiKeysOdataQuery request, CancellationToken cancellationToken)
        {
            var odataResult = await unitOfWork.ApiKeyRepository.OdataGetAsync(request.FilterOptions, x => x.OwnerUserId == currentUserService.GetId() && x.RevokedUtc == null, cancellationToken: cancellationToken);

            var mappedProviders = mapper.Map<IEnumerable<ApiKeyDto>>(odataResult.Value).OrderByDescending(k => k.CreatedUtc);

            var result = new OdataResponse<ApiKeyDto>
            {
                TotalCount = odataResult.TotalCount,
                Value = mappedProviders
            };

            return Result<OdataResponse<ApiKeyDto>>.CreateSuccess(result);
        }
    }
}