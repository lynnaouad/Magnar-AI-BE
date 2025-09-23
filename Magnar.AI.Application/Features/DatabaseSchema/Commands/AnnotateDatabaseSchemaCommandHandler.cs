using Magnar.AI.Application.Configuration;
using Magnar.AI.Application.Dto.Schema;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Interfaces.Managers;
using Magnar.AI.Domain.Entities.Vectors;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Magnar.AI.Application.Features.DatabaseSchema.Commands
{
    public sealed record AnnotateDatabaseSchemaCommand(IEnumerable<TableDto> SelectedTables, int ProviderId) : IRequest<Result>;

    public class AnnotateDatabaseSchemaCommandHandler : IRequestHandler<AnnotateDatabaseSchemaCommand, Result>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        private readonly ISchemaManager schemaManager;
        private readonly IAIManager aiManager;
        private readonly IVectorStoreManager<DatabaseSchemaEmbedding> vectorStore;
        private readonly VectorConfiguration vectoronfiguration;
        private readonly ICurrentUserService currentUserService;
        #endregion

        #region Constructor
        public AnnotateDatabaseSchemaCommandHandler(
            IUnitOfWork unitOfWork,
            IAIManager aiManager,
            IVectorStoreManager<DatabaseSchemaEmbedding> vectorStore,
            IOptions<VectorConfiguration> vectoronfiguration,
            ICurrentUserService currentUserService,
            ISchemaManager schemaManager)
        {
            this.unitOfWork = unitOfWork;
            this.schemaManager = schemaManager;
            this.aiManager = aiManager;
            this.vectorStore = vectorStore;
            this.vectoronfiguration = vectoronfiguration.Value;
            this.currentUserService = currentUserService;
        }
        #endregion

        public async Task<Result> Handle(AnnotateDatabaseSchemaCommand request, CancellationToken cancellationToken)
        {
            var username = currentUserService.GetUsername();

            var provider = await unitOfWork.ProviderRepository.GetAsync(request.ProviderId, false, cancellationToken);
            if (provider is null)
            {
                return Result.CreateFailure([new(Constants.Errors.NotFound)]);
            }

            var canAccessWorkspace = await unitOfWork.WorkspaceRepository.FirstOrDefaultAsync(x => x.CreatedBy == username && x.Id == provider.WorkspaceId, false, cancellationToken);
            if (canAccessWorkspace is null)
            {
                return Result.CreateFailure([new(Constants.Errors.Unauthorized)]);
            }

            await schemaManager.UpsertFileAsync(request.SelectedTables, provider.WorkspaceId, request.ProviderId, cancellationToken);

            if (!vectoronfiguration.EnableVectors)
            {
                return Result.CreateSuccess();
            }

            var allFileTables = await schemaManager.LoadFromFileAsync(provider.WorkspaceId, request.ProviderId, cancellationToken);

            await StoreTablesInVectorStore(allFileTables, request.SelectedTables, provider.WorkspaceId, request.ProviderId, cancellationToken);

            return Result.CreateSuccess();
        }

        #region Private Methods
        private async Task StoreTablesInVectorStore(IEnumerable<TableDto> allTables, IEnumerable<TableDto> requestedTables, int workspaceId, int connectionId, CancellationToken cancellationToken)
        {
            List<DatabaseSchemaEmbedding> embeddings = [];

            var requestedKeys = new HashSet<string>(requestedTables.Select(t => t.FullName), StringComparer.OrdinalIgnoreCase);

            // Set of tables we want to persist
            var tables = allTables.Where(t => requestedKeys.Contains(t.FullName)).ToList();

            // Generate embeddings for the current tables
            var tasks = tables.Select(async table =>
            {
                var serializedText = JsonSerializer.Serialize(table);
                var response = await aiManager.GenerateEmbeddingAsync(serializedText, cancellationToken);
                if (response is null)
                {
                    return null;
                }

                return new DatabaseSchemaEmbedding
                {
                    Id = $"{table.FullName}_{connectionId}",
                    Name = table.FullName,
                    WorkspaceId = workspaceId,
                    ProviderId = connectionId,
                    ChunckIndex = response.Index,
                    Text = response.Text,
                    Embedding = response.Embedding,
                };
            });

            var results = await Task.WhenAll(tasks);
            embeddings.AddRange(results.Where(e => e is not null)!);

            var filters = new Dictionary<string, object>
            {
                { nameof(DatabaseSchemaEmbedding.ProviderId), connectionId.ToString() ?? string.Empty },
            };

            // Get all existing embeddings for this connection
            var existingIdsInStore = await vectorStore.ListIdsAsync(filters, cancellationToken);

            // Determine obsolete embeddings (tables deleted from file)
            var currentIds = allTables.Select(e => e.FullName + "_" + connectionId).ToHashSet();
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
