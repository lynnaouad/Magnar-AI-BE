using Magnar.AI.Application.Dto.Connection;
using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Application.Features.Connection.Queries
{
    public sealed record GetConnectionQuery(int Id) : IRequest<Result<ConnectionDto>>;

    public class GetConnectionQueryHandler : IRequestHandler<GetConnectionQuery, Result<ConnectionDto>>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        #endregion

        #region Constructor
        public GetConnectionQueryHandler(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }
        #endregion

        public async Task<Result<ConnectionDto>> Handle(GetConnectionQuery request, CancellationToken cancellationToken)
        {
            var connection = await unitOfWork.ConnectionRepository.GetConnectionAsync(request.Id, cancellationToken);
            if (connection is null)
            {
                return Result<ConnectionDto>.CreateFailure([new(Constants.Errors.NotFound)]);
            }

            return Result<ConnectionDto>.CreateSuccess(connection);
        }
    }
}
