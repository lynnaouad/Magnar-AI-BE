using Magnar.AI.Application.Dto.Providers;
using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Application.Features.Providers.Queries
{
    public sealed record GetProviderQuery(int Id) : IRequest<Result<ProviderDto>>;

    public class GetProviderQueryHandler : IRequestHandler<GetProviderQuery, Result<ProviderDto>>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        #endregion

        #region Constructor
        public GetProviderQueryHandler(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }
        #endregion

        public async Task<Result<ProviderDto>> Handle(GetProviderQuery request, CancellationToken cancellationToken)
        {
            var provider = await unitOfWork.ProviderRepository.GetProviderAsync(request.Id, cancellationToken);
            if (provider is null)
            {
                return Result<ProviderDto>.CreateFailure([new(Constants.Errors.NotFound)]);
            }

            return Result<ProviderDto>.CreateSuccess(provider);
        }
    }
}
