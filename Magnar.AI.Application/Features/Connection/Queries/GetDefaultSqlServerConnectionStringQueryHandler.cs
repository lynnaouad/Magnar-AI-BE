using Magnar.AI.Application.Dto.Connection;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Microsoft.AspNetCore.DataProtection;

namespace Magnar.AI.Application.Features.Connection.Queries
{
    public sealed record GetDefaultSqlServerConnectionStringQuery() : IRequest<Result<string>>;

    public class GetDefaultSqlServerConnectionStringQueryHandler : IRequestHandler<GetDefaultSqlServerConnectionStringQuery, Result<string>>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        private readonly IDataProtector protector;
        private readonly IMapper mapper;
        #endregion

        #region Constructor
        public GetDefaultSqlServerConnectionStringQueryHandler(IUnitOfWork unitOfWork, IDataProtectionProvider dataProtectorProvider, IMapper mapper)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
            protector = dataProtectorProvider.CreateProtector(Constants.DataProtector.Purpose);
        }
        #endregion

        public async Task<Result<string>> Handle(GetDefaultSqlServerConnectionStringQuery request, CancellationToken cancellationToken)
        {
            var defaultConnection = await unitOfWork.ConnectionRepository.FirstOrDefaultAsync(x => x.IsDefault, false, cancellationToken);
            if(defaultConnection is null)
            {
                return Result<string>.CreateFailure([new(Constants.Errors.NoDefaultConnectionConfigured)]);
            }

            var mapped = mapper.Map<ConnectionDto>(defaultConnection);
            if (mapped.Details?.SqlServerConfiguration is null)
            {
                return Result<string>.CreateFailure([new(Constants.Errors.NoDefaultConnectionConfigured)]);
            }

            var pass = protector.Unprotect(mapped.Details.SqlServerConfiguration.Password);

            var connectionString = $"Server={mapped.Details.SqlServerConfiguration.InstanceName};Database={mapped.Details.SqlServerConfiguration.DatabaseName};User Id={mapped.Details.SqlServerConfiguration.Username};Password={pass};Connection Timeout=30;MultipleActiveResultSets=True;TrustServerCertificate=True";

            return Result<string>.CreateSuccess(connectionString);
        }
    }
}
