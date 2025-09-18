using Magnar.AI.Application.Dto.Providers;
using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Application.Features.Providers.Commands
{
    public sealed record TestProviderCommand(ProviderDto Model) : IRequest<Result<bool>>;

    public class TestProviderCommandHandler : IRequestHandler<TestProviderCommand, Result<bool>>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        #endregion

        #region Constructor
        public TestProviderCommandHandler(IUnitOfWork unitOfWork)
        {
            this.unitOfWork = unitOfWork;
        }
        #endregion

        public async Task<Result<bool>> Handle(TestProviderCommand request, CancellationToken cancellationToken)
        {
            if(request.Model is null)
            {
                return Result<bool>.CreateSuccess(false);
            }

            if(request.Model.Provider == ProviderTypes.SqlServer && request.Model?.Details?.SqlServerConfiguration is not null)
            {
               var sqlTestResult = await unitOfWork.ProviderRepository.TestSqlProviderAsync(request.Model.Details.SqlServerConfiguration, cancellationToken);
               
                return Result<bool>.CreateSuccess(sqlTestResult);
            }

            return Result<bool>.CreateSuccess(false);
        }
    }
}
