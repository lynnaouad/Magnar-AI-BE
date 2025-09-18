using Magnar.AI.Application.Dto.Providers;
using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Application.Features.Providers.Commands
{
    public sealed record CreateProviderCommand(ProviderDto Model) : IRequest<Result>;

    public class CreateProviderCommandHandler : IRequestHandler<CreateProviderCommand, Result>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;
        #endregion

        #region Constructor
        public CreateProviderCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
        }
        #endregion

        public async Task<Result> Handle(CreateProviderCommand request, CancellationToken cancellationToken)
        {
            if (request.Model.Provider == ProviderTypes.SqlServer && request.Model.Details is not null)
            {
                request.Model.Details.SqlServerConfiguration.Password = unitOfWork.ProviderRepository.ProtectPassword(request.Model.Details.SqlServerConfiguration.Password);
            }

            await unitOfWork.ProviderRepository.CreateAsync(mapper.Map<Domain.Entities.Provider>(request.Model), cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.CreateSuccess();
        }
    }
}
