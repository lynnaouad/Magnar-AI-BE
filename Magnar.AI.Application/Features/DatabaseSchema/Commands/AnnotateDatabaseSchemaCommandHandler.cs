using Magnar.AI.Application.Configuration;
using Magnar.AI.Application.Dto.Schema;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Interfaces.Managers;
using Magnar.AI.Domain.Entities.Vectors;
using Microsoft.Extensions.Options;
using System.Linq;
using System.Net.Mail;
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
            var defaultConnection = await unitOfWork.ProviderRepository.FirstOrDefaultAsync(x => x.Type == ProviderTypes.SqlServer, false, cancellationToken);
            if(defaultConnection is null)
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

            await StoreTablesInVectorStore(tables, requests, defaultConnection.Id, cancellationToken);

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

        private async Task StoreTablesInVectorStore(IEnumerable<SelectedTableBlock> allTables, IEnumerable<TableAnnotationRequest> requestedTables, int connectionId, CancellationToken cancellationToken)
        {
            List<DatabaseSchemaEmbedding> embeddings = [];

            // Set of tables we want to persist
            var tables = allTables.Where(t => requestedTables.Select(x => x.FullTableName).Contains(t.FullTableName));

            // Generate embeddings for the current tables
            var tasks = tables.Select(async table =>
            {
                var response = await aiManager.GenerateEmbeddingAsync(table.RawBlockText, cancellationToken);
                if (response is null)
                {
                    return null;
                }

                return new DatabaseSchemaEmbedding
                {
                    Id = $"{table.FullTableName}_{connectionId}",
                    Name = table.FullTableName,
                    ConnectionId = connectionId,
                    ChunckIndex = response.Index,
                    Text = response.Text,
                    Embedding = response.Embedding,
                };
            });

            var results = await Task.WhenAll(tasks);
            embeddings.AddRange(results.Where(e => e is not null)!);

            var filters = new Dictionary<string, object>
            {
                { nameof(DatabaseSchemaEmbedding.ConnectionId), connectionId.ToString() ?? string.Empty },
            };

            // Get all existing embeddings for this connection
            var existingIdsInStore = await vectorStore.ListIdsAsync(filters, cancellationToken);

            // Determine obsolete embeddings (tables deleted from file)
            var currentIds = allTables.Select(e => e.FullTableName + "_" + connectionId).ToHashSet();
            var toDelete = existingIdsInStore.Except(currentIds);

            foreach (var id in toDelete)
            {
                await vectorStore.DeleteAsync(new Dictionary<string, object>
                {
                    { "Id", id }
                }, cancellationToken);
            }

            // Upsert the current embeddings
            await vectorStore.InsertAsync(embeddings, cancellationToken);
        }
        #endregion
    }
}
