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
    public sealed record GenerateAndExecuteSqlQueryCommand(string Prompt, int WorkspaceId) : IRequest<Result<string>>;

    public class GenerateAndExecuteSqlQueryCommandHandler : IRequestHandler<GenerateAndExecuteSqlQueryCommand, Result<string>>
    {
        #region Members
        private readonly IUnitOfWork unitOfWork;
        private IAIManager aiManager;
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
            if (sqlConnection is null)
            {
                return Result<string>.CreateSuccess("Failed! No default database provider configured!");
            }

            // Perform vector search to retrieve tables schema
            var options = new VectorSearchOptions<DatabaseSchemaEmbedding>() { Filter = x => x.ProviderId == sqlConnection.Id };

            VectorSearchResponse<DatabaseSchemaEmbedding> response = await vectorStore.VectorSearchAsync(request.Prompt, 10, options, cancellationToken);
            if (!response.Success || response.SearchResults is null || !response.SearchResults.Any())
            {
                return Result<string>.CreateSuccess("Failed! An error occured while doing a vector search on the available tables");
            }

            // Load assistant messages
            var systemMessage = await PromptLoader.LoadPromptAsync("generate-sql-system.txt", Constants.Folders.GenerateSql);
            var userMessage = await PromptLoader.LoadPromptAsync("generate-sql-user.txt", Constants.Folders.GenerateSql);

            systemMessage = string.Format(systemMessage, string.Join("\n\n", response.SearchResults.Select(r => r.Record.Text)));
            userMessage = string.Format(userMessage, request.Prompt);

            // Generate SQL Query
            var seamanticSearchResult = await aiManager.SemanticSearchAsync(systemMessage, userMessage, cancellationToken);

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

            var connectionString = unitOfWork.ProviderRepository.BuildSqlServerConnectionString(sqlConnection.Details.SqlServerConfiguration);

            try
            {
                var rows = await unitOfWork.ExecuteQueryAsync(result.Sql, connectionString, cancellationToken);

                var json = JsonSerializer.Serialize(rows);

                return Result<string>.CreateSuccess(json);

            }
            catch(Exception ex)
            {
                Log.Error(ex, ex.Message);
                return Result<string>.CreateSuccess("Failed! An error occured while executing the query!");
            }
        }

    }
}
