using DevExpress.Map.Native;
using Magnar.AI.Application.Dto.AI.SemanticSearch;
using Magnar.AI.Application.Helpers;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Interfaces.Managers;
using Magnar.AI.Application.Models.Responses.VectorSearch;
using Magnar.AI.Domain.Entities.Vectors;
using Microsoft.Extensions.VectorData;
using Serilog;
using System.Text.Json;

namespace Magnar.AI.Application.Features.DatabaseSchema.Commands
{
    public sealed record GenerateAndExecuteSqlQueryCommand(string Prompt, int WorkspaceId, bool ExecuteSql = true, DashboardTypes? ChartType = null) : IRequest<Result<string>>;

    public class GenerateAndExecuteSqlQueryCommandHandler : IRequestHandler<GenerateAndExecuteSqlQueryCommand, Result<string>>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        private readonly IAIManager aiManager;
        private readonly IVectorStoreManager<DatabaseSchemaEmbedding> vectorStore;
        #endregion

        #region Constructor
        public GenerateAndExecuteSqlQueryCommandHandler(
            IUnitOfWork unitOfWork,
            IAIManager aiManager,
            IVectorStoreManager<DatabaseSchemaEmbedding> vectorStore)
        {
            this.unitOfWork = unitOfWork;
            this.aiManager = aiManager;
            this.vectorStore = vectorStore;
        }
        #endregion

        public async Task<Result<string>> Handle(GenerateAndExecuteSqlQueryCommand request, CancellationToken cancellationToken)
        {
            // Check connection
            var sqlConnection = await unitOfWork.ProviderRepository.GetDefaultProviderAsync(request.WorkspaceId, ProviderTypes.SqlServer, cancellationToken);
            if (sqlConnection is null || sqlConnection?.Details?.SqlServerConfiguration is null)
            {
                return Result<string>.CreateSuccess("Failed! No default database provider configured!");
            }

            // Perform vector search to retrieve tables schema
            var options = new VectorSearchOptions<DatabaseSchemaEmbedding>() { Filter = x => x.ProviderId == sqlConnection.Id };

            VectorSearchResponse<DatabaseSchemaEmbedding> response = await vectorStore.VectorSearchAsync(request.Prompt, 15, options, cancellationToken);
            if (!response.Success || response.SearchResults is null || !response.SearchResults.Any())
            {
                return Result<string>.CreateSuccess("Failed! An error occured while doing a vector search on the available tables");
            }

            // Load assistant message
            var systemMessage = request.ChartType is not null
                ? await PromptLoader.LoadPromptAsync("generate-dashboard-sql-system.txt", Constants.Folders.DashboardPrompts)
                : await PromptLoader.LoadPromptAsync("generate-sql-system.txt", Constants.Folders.GenerateSql);

            systemMessage = BuildSchemaSystemMessage(systemMessage, DateTime.UtcNow, request.ChartType, response.SearchResults.Select(x => x.Record));

            // Generate SQL Query
            var seamanticSearchResult = await aiManager.SemanticSearchAsync(systemMessage, request.Prompt, cancellationToken);

            DatabaseSchemaSqlDto result;

            try
            {
                result = JsonSerializer.Deserialize<DatabaseSchemaSqlDto>(seamanticSearchResult) ?? new DatabaseSchemaSqlDto();
            }
            catch (Exception ex)
            {
                Log.Error(ex, ex.Message);
                return Result<string>.CreateSuccess("Failed! An error ocured while deserializing the ai response");
            }

            if (!result.Success)
            {
                return Result<string>.CreateSuccess("Failed! Cannot to generate sql query! Kindly check your configured database schema.");
            }

            if (!Utilities.IsSafeSelectQuery(result.Sql))
            {
                return Result<string>.CreateSuccess("Failed! Cannot execute this query! It is not safe!");
            }

            if (request.ExecuteSql)
            {
                try
                {
                    var connectionString = unitOfWork.ProviderRepository.BuildSqlServerConnectionString(sqlConnection.Details.SqlServerConfiguration);

                    var rows = await unitOfWork.ExecuteQueryAsync(result.Sql, connectionString, cancellationToken);

                    var json = JsonSerializer.Serialize(rows);

                    return Result<string>.CreateSuccess(json);

                }
                catch (Exception ex)
                {
                    Log.Error(ex, ex.Message);
                    return Result<string>.CreateSuccess("Failed! An error occured while executing the query!");
                }
            }

            // return sql query
            return Result<string>.CreateSuccess(seamanticSearchResult);
        }

        #region Private Methods
        private string BuildSchemaSystemMessage(string systemTemplate, DateTime utcNow, DashboardTypes? ChartType, IEnumerable<DatabaseSchemaEmbedding> records)
        {
            var ordered = records.ToList();
            var included = new List<string>();

            // Count the system template itself (without schema yet)
            string baseMessage = ChartType is not null 
                ? string.Format(systemTemplate, utcNow, string.Empty, ChartType.ToString())
                : string.Format(systemTemplate, utcNow, string.Empty);

            var tokenCount = aiManager.CountTokenNumber(baseMessage);
            var maxTokens = aiManager.GetModelMaxTokenLimit();

            for (int i = 0; i < ordered.Count; i++)
            {
                int tokens = aiManager.CountTokenNumber(ordered[i].Text ?? string.Empty);

                if (tokenCount + tokens > maxTokens)
                    break; // stop when adding this table would exceed budget

                included.Add(ordered[i].Text);
                tokenCount += tokens;
            }

            // Build final JSON array only with included tables
            var schemaJson = $"[{string.Join(",", included)}]";

            return ChartType is not null 
                ? string.Format(systemTemplate, utcNow, schemaJson, ChartType.ToString())
                : string.Format(systemTemplate, utcNow, schemaJson);
        }
        #endregion
    }
}
