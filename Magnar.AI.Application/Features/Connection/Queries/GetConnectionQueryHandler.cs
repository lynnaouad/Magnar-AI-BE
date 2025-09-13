using Magnar.AI.Application.Dto.Connection;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Microsoft.AspNetCore.DataProtection;

namespace Magnar.AI.Application.Features.Connection.Queries
{
    public sealed record GetConnectionQuery(int Id) : IRequest<Result<ConnectionDto>>;

    public class GetConnectionQueryHandler : IRequestHandler<GetConnectionQuery, Result<ConnectionDto>>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;
        private readonly IDataProtector protector;
        #endregion

        #region Constructor
        public GetConnectionQueryHandler(IUnitOfWork unitOfWork, IMapper mapper, IDataProtectionProvider dataProtectorProvider)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
            protector = dataProtectorProvider.CreateProtector(Constants.DataProtector.Purpose);
        }
        #endregion

        public async Task<Result<ConnectionDto>> Handle(GetConnectionQuery request, CancellationToken cancellationToken)
        {
            var connection = await unitOfWork.ConnectionRepository.GetAsync(request.Id, false, cancellationToken);
            if (connection is null || string.IsNullOrEmpty(connection.Details))
            {
                return Result<ConnectionDto>.CreateFailure([new(Constants.Errors.NotFound)]);
            }

            var mappedConnection = mapper.Map<ConnectionDto>(connection);

            if (mappedConnection.Provider == ProviderTypes.SqlServer && mappedConnection.Details is not null)
            {
                mappedConnection.Details.SqlServerConfiguration.Password = protector.Unprotect(mappedConnection.Details.SqlServerConfiguration.Password);
            }

            return Result<ConnectionDto>.CreateSuccess(mappedConnection);
        }
    }
}
