using Magnar.AI.Application.Dto.AI.SemanticSearch;
using Magnar.AI.Application.Dto.Dashboard;
using Magnar.AI.Application.Helpers;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Interfaces.Managers;
using Magnar.AI.Application.Models.Responses.VectorSearch;
using Magnar.AI.Domain.Entities.Vectors;
using Microsoft.Extensions.VectorData;
using Serilog;
using System.Text.Json;
using System.Xml.Linq;

namespace Magnar.Recruitment.Application.Features.Dashboard.Commands;

public sealed record GenerateDashboardCommand(DashboardPromptDto parameters) : IRequest<Result<string>>;

public class GenerateDashboardCommandHandler : IRequestHandler<GenerateDashboardCommand, Result<string>>
{
    #region Members
    private readonly IDashboardManager dashboardManager;
    private readonly IVectorStoreManager<DatabaseSchemaEmbedding> vectorStore;
    private readonly IAIManager aiManager;
    private readonly IUnitOfWork unitOfWork;
    #endregion

    #region Constructor

   public GenerateDashboardCommandHandler(
        IDashboardManager dashboardManager,
        IVectorStoreManager<DatabaseSchemaEmbedding> vectorStore,
        IAIManager aiManager,
        IUnitOfWork unitOfWork)
    {
        this.dashboardManager = dashboardManager;
        this.vectorStore = vectorStore;
        this.aiManager = aiManager;
        this.unitOfWork = unitOfWork;
    }
    #endregion

    public async Task<Result<string>> Handle(GenerateDashboardCommand request, CancellationToken cancellationToken)
    {
        // Check connection
        var defaultConnection = await unitOfWork.ConnectionRepository.GetDefaultConnectionAsync(cancellationToken);
        if(defaultConnection is null)
        {
            return Result<string>.CreateFailure([new(Constants.Errors.NoDefaultConnectionConfigured)]);
        }

        if(defaultConnection.Provider != ProviderTypes.SqlServer && defaultConnection?.Details?.SqlServerConfiguration != null)
        {
            return Result<string>.CreateFailure([new(Constants.Errors.CannotGenerateDashboard)]);
        }

        // Perform vector search to retrieve tables schema
        var options = new VectorSearchOptions<DatabaseSchemaEmbedding>() { Filter = x => x.ConnectionId == defaultConnection.Id };

        VectorSearchResponse<DatabaseSchemaEmbedding> response = await vectorStore.VectorSearchAsync(request.parameters.Prompt, 10, options, cancellationToken);
        if (!response.Success || response.SearchResults is null || !response.SearchResults.Any())
        {
            return Result<string>.CreateFailure([new(Constants.Errors.CannotGenerateDashboard)]);
        }

        // Load prompt messages
        var systemMessage = await PromptLoader.LoadPromptAsync("generate-dashboard-sql-system.txt", Constants.Folders.DashboardPrompts);
        var userMessage = await PromptLoader.LoadPromptAsync("generate-dashboard-sql-user.txt", Constants.Folders.DashboardPrompts);

        systemMessage = string.Format(systemMessage, string.Join("\n\n", response.SearchResults.Select(r => r.Record.Text)), request.parameters.ChartType);
        userMessage = string.Format(userMessage, request.parameters.Prompt);

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
            return Result<string>.CreateFailure([new(Constants.Errors.GenerateDashboardError)]);
        }

        if (!result.Success)
        {
            return Result<string>.CreateFailure([new(Constants.Errors.CannotGenerateDashboardUpdateSchema)]);
        }

        if (!IsSafeSelectQuery(result.Sql))
        {
            return Result<string>.CreateFailure([new(Constants.Errors.CannotGenerateDashboard)]);
        }

        var dashboard = dashboardManager.CreateDashboard(defaultConnection.Details.SqlServerConfiguration, result.Sql, request.parameters.ChartType, result.Columns);

        XDocument xdoc = dashboard.SaveToXDocument();

        var dashboardId = $"AI_{Guid.NewGuid():N}";

        // Claen old dashboards from memory
        dashboardManager.RemoveAllForCurrentUser();

        dashboardManager.SaveDashboard(dashboardId, xdoc);

        return Result<string>.CreateSuccess(dashboardId);
    }

    #region Private Methods
    private bool IsSafeSelectQuery(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return false;
        }

        // Normalize for comparison
        string upperSql = sql.Trim().ToUpperInvariant();

        // Must start with SELECT
        if (!upperSql.StartsWith("SELECT"))
        {
            return false;
        }

        // Disallow dangerous commands anywhere
        string[] forbidden = { "INSERT", "UPDATE", "DELETE", "EXEC", "MERGE", "DROP", "ALTER", "TRUNCATE" };

        foreach (var keyword in forbidden)
        {
            if (upperSql.Contains(keyword + " ")) // space prevents matching substrings like 'EXECUTE'
            {
                return false;
            }
        }

        return true;
    }
    #endregion
}
