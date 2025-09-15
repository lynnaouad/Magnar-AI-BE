using Magnar.AI.Application.Dto.Connection;
using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Application.Features.Connection.Commands
{
    public sealed record TestConnectionCommand(ConnectionDto Connection) : IRequest<Result<bool>>;

    public class TestConnectionCommandHandler : IRequestHandler<TestConnectionCommand, Result<bool>>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        #endregion

        #region Constructor
        public TestConnectionCommandHandler(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }
        #endregion

        public async Task<Result<bool>> Handle(TestConnectionCommand request, CancellationToken cancellationToken)
        {
            if(request.Connection is null)
            {
                return Result<bool>.CreateSuccess(false);
            }

            if(request.Connection.Provider == ProviderTypes.SqlServer && request.Connection?.Details?.SqlServerConfiguration is not null)
            {
               var sqlTestResult = await unitOfWork.ConnectionRepository.TestSqlConnectionAsync(request.Connection.Details.SqlServerConfiguration, cancellationToken);
               
                return Result<bool>.CreateSuccess(sqlTestResult);
            }

            return Result<bool>.CreateSuccess(false);
        }
    }
}
