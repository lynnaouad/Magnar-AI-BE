using Magnar.AI.Application.Dto.ApiKeys;
using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Application.Features.ApiKeys.Queries
{
    public sealed record GetApiKeysQuery() : IRequest<Result<IEnumerable<ApiKeyDto>>>;

    public class GetApiKeysQueryHandler : IRequestHandler<GetApiKeysQuery, Result<IEnumerable<ApiKeyDto>>>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;
        private readonly ICurrentUserService currentUserService;
        #endregion

        #region Constructor

        public GetApiKeysQueryHandler(IUnitOfWork unitOfWork, IMapper mapper, ICurrentUserService currentUserService)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
            this.currentUserService = currentUserService;
        }
        #endregion

        public async Task<Result<IEnumerable<ApiKeyDto>>> Handle(GetApiKeysQuery request, CancellationToken cancellationToken)
        {
            var list = (await unitOfWork.ApiKeyRepository
                .WhereAsync(k => k.OwnerUserId == currentUserService.GetId() && k.RevokedUtc == null, false, cancellationToken))
                .OrderByDescending(k => k.CreatedUtc);

            return Result<IEnumerable<ApiKeyDto>>.CreateSuccess(mapper.Map<IEnumerable<ApiKeyDto>>(list));
        }
    }
}