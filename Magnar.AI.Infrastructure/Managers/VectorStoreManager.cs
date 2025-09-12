using Magnar.AI.Application.Configuration;
using Magnar.AI.Application.Interfaces.Managers;
using Magnar.AI.Application.Models.Responses.VectorSearch;
using Magnar.AI.Entities.Abstraction;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.VectorData;
using Microsoft.SemanticKernel.Connectors.SqlServer;
using Serilog;

namespace Magnar.Recruitment.Infrastructure.Managers;

public class VectorStoreManager<T> : IVectorStoreManager<T>
     where T : VectorStoreBase
{
    #region Members
    private readonly VectorConfiguration vectorConfiguration;
    private readonly IAIManager aiManager;

    private readonly string connectionString;
    private readonly SqlServerCollection<Guid, T> collection;
    #endregion

    #region Constructor
    public VectorStoreManager(
        IOptions<VectorConfiguration> vectorConfiguration,
        IAIManager aiManager,
        IConfiguration configuration,
        SqlServerVectorStore vectorStore)
    {
        this.vectorConfiguration = vectorConfiguration.Value;
        this.aiManager = aiManager;

        connectionString = configuration?.GetConnectionString(Constants.Database.DefaultConnectionString) ?? string.Empty;
        collection = vectorStore.GetCollection<Guid, T>(typeof(T).Name);
    }
    #endregion

    public async Task InsertAsync(IEnumerable<T> list, CancellationToken cancellationToken = default)
    {
        if (!vectorConfiguration.EnableVectors || !list.Any())
        {
            return;
        }

        try
        {
            await collection.EnsureCollectionExistsAsync(cancellationToken);

            await collection.UpsertAsync(list, cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, ex.Message);
        }
    }

    public async Task DeleteTableAsync(CancellationToken cancellationToken = default)
    {
        if (!vectorConfiguration.EnableVectors)
        {
            return;
        }

        try
        {
            await collection.EnsureCollectionDeletedAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, ex.Message);
        }
    }

    public async Task<VectorSearchResponse<T>> VectorSearchAsync(string prompt, int top, VectorSearchOptions<T> searchOptions, CancellationToken cancellationToken = default)
    {
        try
        {
            if (!vectorConfiguration.EnableVectors)
            {
                return new VectorSearchResponse<T>() { Success = false, Error = new(Constants.Errors.VectorSearchNotEnabled) };
            }

            var promptEmbedding = await aiManager.GenerateEmbeddingAsync(prompt, cancellationToken);
            if (promptEmbedding is null || top < 1)
            {
                return new VectorSearchResponse<T>() { Success = true, SearchResults = [] };
            }

            await collection.EnsureCollectionExistsAsync(cancellationToken);

            var searchResults = await collection.SearchAsync(promptEmbedding.Embedding, top, searchOptions, cancellationToken: cancellationToken).ToListAsync(cancellationToken);

            return new VectorSearchResponse<T>() { Success = true, SearchResults = searchResults };
        }
        catch (Exception ex)
        {
            Log.Error(ex, ex.Message);

            return new VectorSearchResponse<T>() { Success = false, Error = new(Constants.Errors.VectorSearchNotEnabled) };
        }
    }

    public async Task DeleteAsync(Dictionary<string, object> filters, CancellationToken cancellationToken = default)
    {
        if (!vectorConfiguration.EnableVectors || filters is null || filters.Count == 0 || !await collection.CollectionExistsAsync(cancellationToken))
        {
            return;
        }

        try
        {
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);

            var command = connection.CreateCommand();
            var conditions = new List<string>();
            int index = 0;

            foreach (var kvp in filters)
            {
                var paramName = $"@p{index++}";
                command.Parameters.AddWithValue(paramName, kvp.Value ?? DBNull.Value);
                conditions.Add($"{kvp.Key} = {paramName}");
            }

            var collectionName = typeof(T).Name;

            command.CommandText = $@"
            DELETE FROM [{Constants.Database.Schemas.Default}].[{collectionName}]
            WHERE {string.Join(" AND ", conditions)}";

            await command.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            Log.Error(ex, ex.Message);
        }
    }
}
