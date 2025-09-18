using Magnar.AI.Application.Dto.Providers;
using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Application.Features.Providers.Commands
{
    public sealed record UpdateProviderCommand(ProviderDto Model) : IRequest<Result>;

    public class UpdateProviderCommandHandler : IRequestHandler<UpdateProviderCommand, Result>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        private readonly IMapper mapper;
        #endregion

        #region Constructor
        public UpdateProviderCommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
        {
            this.unitOfWork = unitOfWork;
            this.mapper = mapper;
        }
        #endregion

        public async Task<Result> Handle(UpdateProviderCommand request, CancellationToken cancellationToken)
        {
            if (request.Model.Provider == ProviderTypes.SqlServer && request.Model.Details is not null)
            {
                var protectedPassword = unitOfWork.ProviderRepository.ProtectPassword(request.Model.Details.SqlServerConfiguration.Password);

                request.Model.Details.SqlServerConfiguration.Password = protectedPassword;
            }

            unitOfWork.ProviderRepository.Update(mapper.Map<Provider>(request.Model));

            await unitOfWork.SaveChangesAsync(cancellationToken);

            return Result.CreateSuccess();
        }
    }
}
