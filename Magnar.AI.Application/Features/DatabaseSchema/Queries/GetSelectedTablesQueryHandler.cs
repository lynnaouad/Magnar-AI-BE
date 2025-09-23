using Magnar.AI.Application.Dto.Schema;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Interfaces.Managers;
using Magnar.AI.Domain.Entities;

namespace Magnar.AI.Application.Features.DatabaseSchema.Queries
{
    public sealed record GetSelectedTablesQuery(int ProviderId) : IRequest<Result<IEnumerable<TableDto>>>;

    public class GetSelectedTablesQueryHandler : IRequestHandler<GetSelectedTablesQuery, Result<IEnumerable<TableDto>>>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        private readonly ISchemaManager schemaManager;
        private readonly ICurrentUserService currentUserService;
        #endregion

        #region Constructor
        public GetSelectedTablesQueryHandler( IUnitOfWork unitOfWork, ISchemaManager schemaManager, ICurrentUserService currentUserService)
        {
            this.unitOfWork = unitOfWork;
            this.schemaManager = schemaManager;
            this.currentUserService = currentUserService;
        }
        #endregion

        public async Task<Result<IEnumerable<TableDto>>> Handle(GetSelectedTablesQuery request, CancellationToken cancellationToken)
        {
            var username = currentUserService.GetUsername();

            var provider = await unitOfWork.ProviderRepository.GetAsync(request.ProviderId, false, cancellationToken);
            if(provider is null)
            {
                return Result<IEnumerable<TableDto>>.CreateFailure([new(Constants.Errors.NotFound)]);
            }

            var canAccessWorkspace = await unitOfWork.WorkspaceRepository.FirstOrDefaultAsync(x => x.CreatedBy == username && x.Id == provider.WorkspaceId, false, cancellationToken);
            if (canAccessWorkspace is null)
            {
                return Result<IEnumerable<TableDto>>.CreateFailure([new(Constants.Errors.Unauthorized)]);
            }

            await schemaManager.RemoveMissingTablesAsync(provider.WorkspaceId, request.ProviderId, cancellationToken);

            var list = await schemaManager.MergeSelectionsFromFileAsync(provider.WorkspaceId, request.ProviderId, cancellationToken);

            return Result<IEnumerable<TableDto>>.CreateSuccess(list);
        }
    }
}
