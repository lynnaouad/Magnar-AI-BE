using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Application.Features.ApiKeys.Commands
{
    public sealed record CreateApiKeyCommand(CreateApiKeyDto dto) : IRequest<Result<string>>;

    public class CreateApiKeyCommandHandler : IRequestHandler<CreateApiKeyCommand, Result<string>>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        private readonly ICurrentUserService currentUserService;
        #endregion

        #region Constructor

        public CreateApiKeyCommandHandler(IUnitOfWork unitOfWork, ICurrentUserService currentUserService)
        {
            this.unitOfWork = unitOfWork;
            this.currentUserService = currentUserService;
        }
        #endregion

        public async Task<Result<string>> Handle(CreateApiKeyCommand request, CancellationToken cancellationToken)
        {
            var scopes = string.IsNullOrWhiteSpace(request.dto.Scopes) 
                ? [Constants.IdentityApi.ApiScopeNames.Full, Constants.IdentityApi.ApiScopeNames.Read, Constants.IdentityApi.ApiScopeNames.Modify, Constants.IdentityApi.ApiScopeNames.Write] 
                : request.dto.Scopes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            
            TimeSpan? lifetime = request.dto.TtlMinutes.HasValue ? TimeSpan.FromMinutes(request.dto.TtlMinutes.Value) : null;

            (string plain, ApiKey entity) = await unitOfWork.ApiKeyRepository.CreateAsync(currentUserService.GetId(), string.Empty, scopes, lifetime, request.dto.Name, request.dto.MetadataJson);
           
            return Result<string>.CreateSuccess(plain);
        }
    }
}