using Magnar.AI.Application.Dto.Connection;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Microsoft.AspNetCore.DataProtection;

namespace Magnar.AI.Application.Features.Connection.Commands
{
    public sealed record CreateConnectionCommand(ConnectionDto Connection) : IRequest<Result>;

    public class CreateConnectionCommandHandler : IRequestHandler<CreateConnectionCommand, Result>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;
        private readonly IDataProtector protector;
        #endregion

        #region Constructor
        public CreateConnectionCommandHandler(IUnitOfWork unitOfWork, IMapper mapper, IDataProtectionProvider dataProtectorProvider)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
            protector = dataProtectorProvider.CreateProtector(Constants.DataProtector.Purpose);
        }
        #endregion

        public async Task<Result> Handle(CreateConnectionCommand request, CancellationToken cancellationToken)
        {
            if (request.Connection.Provider == ProviderTypes.SqlServer)
            {
                request.Connection.Details.SqlServerConfiguration.Password = protector.Protect(request.Connection.Details.SqlServerConfiguration.Password);
            }

            var newConnection = mapper.Map<Domain.Entities.Connection>(request.Connection);
           
            await unitOfWork.ConnectionRepository.CreateAsync(newConnection, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.CreateSuccess();
        }
    }
}
