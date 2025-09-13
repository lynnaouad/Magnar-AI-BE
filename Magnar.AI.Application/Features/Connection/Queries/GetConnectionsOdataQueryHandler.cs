using Magnar.AI.Application.Dto.Connection;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Models.Responses;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.OData.Query;

namespace Magnar.AI.Application.Features.Connection.Queries
{
    public sealed record GetConnectionsOdataQuery(ODataQueryOptions<Domain.Entities.Connection> FilterOptions) : IRequest<Result<OdataResponse<ConnectionDto>>>;

    public class GetConnectionsOdataQueryHandler : IRequestHandler<GetConnectionsOdataQuery, Result<OdataResponse<ConnectionDto>>>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;
        private readonly IDataProtector protector;
        #endregion

        #region Constructor
        public GetConnectionsOdataQueryHandler(IUnitOfWork unitOfWork, IMapper mapper, IDataProtectionProvider dataProtectorProvider)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
            protector = dataProtectorProvider.CreateProtector(Constants.DataProtector.Purpose);
        }
        #endregion

        public async Task<Result<OdataResponse<ConnectionDto>>> Handle(GetConnectionsOdataQuery request, CancellationToken cancellationToken)
        {
            var result = await unitOfWork.ConnectionRepository.OdataGetAsync(request.FilterOptions, cancellationToken: cancellationToken);

            var mappedConnections = mapper.Map<IEnumerable<ConnectionDto>>(result.Value);

            mappedConnections = mappedConnections.Select(x =>
            {
                if (x.Provider == ProviderTypes.SqlServer)
                {
                    x.Details.SqlServerConfiguration.Password = protector.Unprotect(x.Details.SqlServerConfiguration.Password);
                }

                return x;
            });

            var mappedResult = new OdataResponse<ConnectionDto>
            {
                TotalCount = result.TotalCount,
                Value = mappedConnections
            };

            return Result<OdataResponse<ConnectionDto>>.CreateSuccess(mappedResult);
        }
    }
}
