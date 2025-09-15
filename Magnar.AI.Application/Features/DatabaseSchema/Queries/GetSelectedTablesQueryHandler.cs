using Magnar.AI.Application.Dto.Schema;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Interfaces.Managers;

namespace Magnar.AI.Application.Features.DatabaseSchema.Queries
{
    public sealed record GetSelectedTablesQuery() : IRequest<Result<IEnumerable<SelectedTableBlock>>>;

    public class GetSelectedTablesQueryHandler : IRequestHandler<GetSelectedTablesQuery, Result<IEnumerable<SelectedTableBlock>>>
    {
        #region Members
        private readonly IAnnotationFileManager annotationFileManager;
        private readonly IUnitOfWork unitOfWork;
        private readonly ISchemaManager schemaManager;
        #endregion

        #region Constructor
        public GetSelectedTablesQueryHandler(IAnnotationFileManager annotationFileManager, IUnitOfWork unitOfWork, ISchemaManager schemaManager)
        {
            this.annotationFileManager = annotationFileManager;
            this.unitOfWork = unitOfWork;
            this.schemaManager = schemaManager;
        }
        #endregion

        public async Task<Result<IEnumerable<SelectedTableBlock>>> Handle(GetSelectedTablesQuery request, CancellationToken cancellationToken)
        {
            var defaultConnection = await unitOfWork.ConnectionRepository.FirstOrDefaultAsync(x => x.IsDefault, false, cancellationToken);
            if (defaultConnection is null || defaultConnection.Provider != ProviderTypes.SqlServer)
            {
                return Result<IEnumerable<SelectedTableBlock>>.CreateFailure([new(Constants.Errors.NoDefaultConnectionConfigured)]);
            }

            // Get All database tables
            var databaseTablesResult = await schemaManager.GetTablesAsync(cancellationToken);
            if (!databaseTablesResult.Success)
            {
                return Result<IEnumerable<SelectedTableBlock>>.CreateFailure([new(Constants.Errors.ErrorOccured)]);
            }

            // Clean old tables that may be deleted or renamed
            var existingDbTables = databaseTablesResult.Value.Select(t => $"[{t.SchemaName}].[{t.TableName}]");
            await annotationFileManager.CleanupOrphanedBlocksAsync(defaultConnection.Id, existingDbTables);

            // Get selected tables
            var result = await annotationFileManager.ReadAllBlocksAsync(defaultConnection.Id);

            return Result<IEnumerable<SelectedTableBlock>>.CreateSuccess(result);
        }
    }
}
