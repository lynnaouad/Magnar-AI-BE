using Magnar.AI.Application.Dto.Connection;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Models.Responses;
using Microsoft.AspNetCore.OData.Query;

namespace Magnar.AI.Application.Features.Connection.Queries
{
    public sealed record GetConnectionsOdataQuery(ODataQueryOptions<Domain.Entities.Connection> FilterOptions) : IRequest<Result<OdataResponse<ConnectionDto>>>;

    public class GetConnectionsOdataQueryHandler : IRequestHandler<GetConnectionsOdataQuery, Result<OdataResponse<ConnectionDto>>>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        #endregion

        #region Constructor
        public GetConnectionsOdataQueryHandler(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }
        #endregion

        public async Task<Result<OdataResponse<ConnectionDto>>> Handle(GetConnectionsOdataQuery request, CancellationToken cancellationToken)
        {
            var result = await unitOfWork.ConnectionRepository.GetConnectionsOdataAsync(request.FilterOptions, cancellationToken);

            return Result<OdataResponse<ConnectionDto>>.CreateSuccess(result);
        }
    }
}
