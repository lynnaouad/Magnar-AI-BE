using Magnar.AI.Application.Configuration;
using Magnar.AI.Application.Dto.Schema;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Interfaces.Managers;
using Magnar.AI.Domain.Entities.Vectors;
using Microsoft.Extensions.Options;
using System.Text.RegularExpressions;

namespace Magnar.AI.Application.Features.DatabaseSchema.Commands
{
    public sealed record AnnotateDatabaseSchemaCommand(IEnumerable<TableAnnotationRequest> TableAnnotation) : IRequest<Result>;

    public class AnnotateDatabaseSchemaCommandHandler : IRequestHandler<AnnotateDatabaseSchemaCommand, Result>
    {
        #region Members
        private readonly IAnnotationFileManager annotationFileManager;
        private readonly IUnitOfWork unitOfWork;
        private ISchemaManager schemaManager;
        private IAIManager aiManager;
        private readonly IVectorStoreManager<DatabaseSchemaEmbedding> vectorStore;
        private readonly VectorConfiguration vectoronfiguration;
        #endregion

        #region Constructor
        public AnnotateDatabaseSchemaCommandHandler(
            IAnnotationFileManager annotationFileManager,
            IUnitOfWork unitOfWork,
            IAIManager aiManager,
            IVectorStoreManager<DatabaseSchemaEmbedding> vectorStore,
            IOptions<VectorConfiguration> vectoronfiguration,
            ISchemaManager schemaManager)
        {
            this.annotationFileManager = annotationFileManager;
            this.unitOfWork = unitOfWork;
            this.schemaManager = schemaManager;
            this.aiManager = aiManager;
            this.vectorStore = vectorStore;
            this.vectoronfiguration = vectoronfiguration.Value;
        }
        #endregion

        public async Task<Result> Handle(AnnotateDatabaseSchemaCommand request, CancellationToken cancellationToken)
        {
            var defaultConnection = await unitOfWork.ConnectionRepository.FirstOrDefaultAsync(x => x.IsDefault, false, cancellationToken);
            if(defaultConnection is null || defaultConnection.Provider != ProviderTypes.SqlServer)
            {
                return Result.CreateFailure([new(Constants.Errors.NoDefaultConnectionConfigured)]);
            }

            var requests = await HandleBulkSelectRequests(request.TableAnnotation, cancellationToken);

            await annotationFileManager.AppendOrReplaceBlocksAsync(requests, defaultConnection.Id);

            if (!vectoronfiguration.EnableVectors)
            {
                return Result.CreateSuccess();
            }
           
            var tables = await annotationFileManager.ReadAllBlocksAsync(defaultConnection.Id);

            await StoreTablesInVectorStore(tables, defaultConnection.Id, cancellationToken);

            return Result.CreateSuccess();
        }

        #region Private Methods

        // In Bulk select the table info are not retrieved only the table name
        // This method maps the table with its columns in case of bulk select
        private async Task<IEnumerable<TableAnnotationRequest>> HandleBulkSelectRequests(IEnumerable<TableAnnotationRequest> requests, CancellationToken cancellationToken)
        {
            var enrichedRequests = new List<TableAnnotationRequest>();

            foreach (var req in requests)
            {
                // Parse schema and table name from fullName
                var m = Regex.Match(req.FullTableName, @"\[(?<schema>[^\]]+)\]\.\[(?<table>[^\]]+)\]");
                if (!m.Success)
                {
                    continue;
                }

                var schema = m.Groups["schema"].Value;
                var table = m.Groups["table"].Value;

                // Retrieve table info from SchemaManager
                var tableInfoResult = await schemaManager.GetTableInfoAsync(schema, table, cancellationToken);
                if (!tableInfoResult.Success)
                {
                    continue;
                }

                var tableInfo = tableInfoResult.Value;

                // If no columns in request, fill them from DB
                var colComments = req.ColumnComments?.Any() == true
                    ? req.ColumnComments
                    : tableInfo.Columns.ToDictionary(c => c.ColumnName, _ => (string?)string.Empty);

                enrichedRequests.Add(new TableAnnotationRequest
                {
                    FullTableName = req.FullTableName,
                    TableDescription = req.TableDescription,
                    ColumnComments = colComments
                });
            }

            return enrichedRequests;
        }

        // Save entire file in vector store on every update to handle table removals or renaming
        private async Task StoreTablesInVectorStore(IEnumerable<SelectedTableBlock> tables, int connectionId, CancellationToken cancellationToken)
        {
            List<DatabaseSchemaEmbedding> embeddings = [];

            var tasks = tables.Select(async table =>
            {
                var response = await aiManager.GenerateEmbeddingAsync(table.RawBlockText, cancellationToken);
                if (response is null)
                {
                    return null;
                }

                return new
                {
                    Table = table,
                    Response = response
                };
            });

            var results = await Task.WhenAll(tasks);

            foreach (var result in results.Where(r => r is not null))
            {
                embeddings.Add(new()
                {
                    ID = Guid.NewGuid(),
                    Name = result.Table.FullTableName,
                    ConnectionId = connectionId,
                    ChunckIndex = result.Response.Index,
                    Text = result.Response.Text,
                    Embedding = result.Response.Embedding,
                });
            }

            await vectorStore.DeleteTableAsync(cancellationToken);
            await vectorStore.InsertAsync(embeddings, cancellationToken);
        }
        #endregion
    }
}
