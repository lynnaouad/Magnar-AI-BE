using Magnar.AI.Application.Dto.AI.SemanticSearch;
using Magnar.AI.Application.Dto.Dashboard;
using Magnar.AI.Application.Helpers;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Interfaces.Managers;
using Magnar.AI.Application.Models.Responses.VectorSearch;
using Magnar.AI.Application.Services;
using Magnar.AI.Domain.Entities.Vectors;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.VectorData;
using Serilog;
using System.Text.Json;
using System.Xml.Linq;

namespace Magnar.Recruitment.Application.Features.Dashboard.Commands;

public sealed record GenerateDashboardCommand(DashboardPromptDto Parameters) : IRequest<Result<string>>;

public class GenerateDashboardCommandHandler : IRequestHandler<GenerateDashboardCommand, Result<string>>
{
    #region Members
    private readonly IDashboardManager dashboardManager;
    private readonly IVectorStoreManager<DatabaseSchemaEmbedding> vectorStore;
    private readonly IAIManager aiManager;
    private readonly IUnitOfWork unitOfWork;
    private readonly ICurrentUserService currentUserService;
    private readonly IAuthorizationService authorizationService;
    #endregion

    #region Constructor

    public GenerateDashboardCommandHandler(
        IDashboardManager dashboardManager,
        IVectorStoreManager<DatabaseSchemaEmbedding> vectorStore,
        ICurrentUserService currentUserService,
        IAIManager aiManager,
        IAuthorizationService authorizationService,
        IUnitOfWork unitOfWork)
    {
        this.dashboardManager = dashboardManager;
        this.vectorStore = vectorStore;
        this.aiManager = aiManager;
        this.unitOfWork = unitOfWork;
        this.currentUserService = currentUserService;
        this.authorizationService = authorizationService;
    }
    #endregion

    public async Task<Result<string>> Handle(GenerateDashboardCommand request, CancellationToken cancellationToken)
    {
        var canAccessWorkspace = await authorizationService.CanAccessWorkspace(request.Parameters.WorkspaceId, cancellationToken);
        if (!canAccessWorkspace)
        {
            return Result<string>.CreateFailure([new(Constants.Errors.Unauthorized)], StatusCodes.Status401Unauthorized);
        }

        // Check connection
        var sqlConnection = await unitOfWork.ProviderRepository.GetDefaultProviderAsync(request.Parameters.WorkspaceId, ProviderTypes.SqlServer, cancellationToken);
        if (sqlConnection is null || sqlConnection?.Details?.SqlServerConfiguration is null)
        {
            return Result<string>.CreateFailure([new(Constants.Errors.NoDefaultConnectionConfigured)]);
        }

        // Perform vector search to retrieve tables schema
        var options = new VectorSearchOptions<DatabaseSchemaEmbedding>() { Filter = x => x.ProviderId == sqlConnection.Id };

        VectorSearchResponse<DatabaseSchemaEmbedding> response = await vectorStore.VectorSearchAsync(request.Parameters.Prompt, 10, options, cancellationToken);
        if (!response.Success || response.SearchResults is null || !response.SearchResults.Any())
        {
            return Result<string>.CreateFailure([new(Constants.Errors.CannotGenerateDashboard)]);
        }

        // Load assistant messages
        var systemMessage = await PromptLoader.LoadPromptAsync("generate-dashboard-sql-system.txt", Constants.Folders.DashboardPrompts);
        var userMessage = await PromptLoader.LoadPromptAsync("generate-dashboard-sql-user.txt", Constants.Folders.DashboardPrompts);

        systemMessage = string.Format(systemMessage, string.Join("\n\n", response.SearchResults.Select(r => r.Record.Text)), request.Parameters.ChartType);
        userMessage = string.Format(userMessage, request.Parameters.Prompt);

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

        if (!Utilities.IsSafeSelectQuery(result.Sql))
        {
            return Result<string>.CreateFailure([new(Constants.Errors.CannotGenerateDashboard)]);
        }

        var dashboard = dashboardManager.CreateDashboard(sqlConnection.Details.SqlServerConfiguration, result.Sql, request.Parameters.ChartType, result.Columns);

        XDocument xdoc = dashboard.SaveToXDocument();

        var dashboardId = $"AI_{Guid.NewGuid():N}";

        // Claen old dashboards from memory
        dashboardManager.RemoveAllForCurrentUser(request.Parameters.WorkspaceId);

        var fullkey = dashboardManager.SaveDashboard(request.Parameters.WorkspaceId, currentUserService.GetUsername(), dashboardId, xdoc);

        return Result<string>.CreateSuccess(fullkey);
    }
}
