using Magnar.AI.Application.Dto.Connection;
using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Application.Features.Connection.Commands
{
    public sealed record UpdateConnectionCommand(ConnectionDto Connection) : IRequest<Result>;

    public class UpdateConnectionCommandHandler : IRequestHandler<UpdateConnectionCommand, Result>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;
        #endregion

        #region Constructor
        public UpdateConnectionCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
        }
        #endregion

        public async Task<Result> Handle(UpdateConnectionCommand request, CancellationToken cancellationToken)
        {
            if (request.Connection.Provider == ProviderTypes.SqlServer && request.Connection.Details is not null)
            {
                var protectedPassword = unitOfWork.ConnectionRepository.ProtectPassword(request.Connection.Details.SqlServerConfiguration.Password);

                request.Connection.Details.SqlServerConfiguration.Password = protectedPassword;
            }

            unitOfWork.ConnectionRepository.Update(mapper.Map<Domain.Entities.Connection>(request.Connection));

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.CreateSuccess();
        }
    }
}
