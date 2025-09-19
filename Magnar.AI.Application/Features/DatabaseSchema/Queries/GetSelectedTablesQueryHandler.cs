using Magnar.AI.Application.Dto.Schema;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Interfaces.Managers;

namespace Magnar.AI.Application.Features.DatabaseSchema.Queries
{
    public sealed record GetSelectedTablesQuery(int WorkspaceId, int ProviderId) : IRequest<Result<IEnumerable<TableDto>>>;

    public class GetSelectedTablesQueryHandler : IRequestHandler<GetSelectedTablesQuery, Result<IEnumerable<TableDto>>>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        private readonly ISchemaManager schemaManager;
        private readonly IMapper mapper;
        #endregion

        #region Constructor
        public GetSelectedTablesQueryHandler( IUnitOfWork unitOfWork, ISchemaManager schemaManager, IMapper mapper)
        {
            this.unitOfWork = unitOfWork;
            this.schemaManager = schemaManager;
            this.mapper = mapper;
        }
        #endregion

        public async Task<Result<IEnumerable<TableDto>>> Handle(GetSelectedTablesQuery request, CancellationToken cancellationToken)
        {
            await schemaManager.RemoveMissingTablesAsync(request.WorkspaceId, request.ProviderId, cancellationToken);

            var list = await schemaManager.MergeSelectionsFromFileAsync(request.WorkspaceId, request.ProviderId, cancellationToken);

            return Result<IEnumerable<TableDto>>.CreateSuccess(list);
        }
    }
}
