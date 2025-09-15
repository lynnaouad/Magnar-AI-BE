using Magnar.AI.Application.Dto.Connection;
using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Application.Features.Connection.Commands
{
    public sealed record CreateConnectionCommand(ConnectionDto Connection) : IRequest<Result>;

    public class CreateConnectionCommandHandler : IRequestHandler<CreateConnectionCommand, Result>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;
        #endregion

        #region Constructor
        public CreateConnectionCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
        }
        #endregion

        public async Task<Result> Handle(CreateConnectionCommand request, CancellationToken cancellationToken)
        {
            if (request.Connection.Provider == ProviderTypes.SqlServer && request.Connection.Details is not null)
            {
                request.Connection.Details.SqlServerConfiguration.Password = unitOfWork.ConnectionRepository.ProtectPassword(request.Connection.Details.SqlServerConfiguration.Password);
            }

            await unitOfWork.ConnectionRepository.CreateAsync(mapper.Map<Domain.Entities.Connection>(request.Connection), cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.CreateSuccess();
        }
    }
}
