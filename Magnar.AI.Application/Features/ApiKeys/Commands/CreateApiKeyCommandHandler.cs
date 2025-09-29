using Magnar.AI.Application.Dto.ApiKeys;
using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Application.Features.ApiKeys.Commands
{
    public sealed record CreateApiKeyCommand(ApiKeyParametersDto Dto) : IRequest<Result<string>>;

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
            var scopes = string.IsNullOrWhiteSpace(request.Dto.Scopes) 
                ? [Constants.IdentityApi.ApiScopeNames.Full, Constants.IdentityApi.ApiScopeNames.Read, Constants.IdentityApi.ApiScopeNames.Modify, Constants.IdentityApi.ApiScopeNames.Write] 
                : request.Dto.Scopes.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            
            TimeSpan? lifetime = request.Dto.TtlMinutes.HasValue ? TimeSpan.FromMinutes(request.Dto.TtlMinutes.Value) : null;

            (string plain, ApiKey entity) = await unitOfWork.ApiKeyRepository.CreateAsync(currentUserService.GetId(), request.Dto.TenantId, scopes, lifetime, request.Dto.Name, request.Dto.MetadataJson);
           
            return Result<string>.CreateSuccess(plain);
        }
    }
}