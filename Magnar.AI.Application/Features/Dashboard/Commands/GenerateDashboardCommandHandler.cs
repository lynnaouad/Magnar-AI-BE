using DevExpress.DashboardCommon;
using DevExpress.DataAccess.ConnectionParameters;
using DevExpress.DataAccess.Sql;
using Magnar.AI.Application.Dashboards;
using Magnar.AI.Application.Dto.AI.SemanticSearch;
using Magnar.AI.Application.Dto.Connection;
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
    private readonly UserScopedDashboardStorage dashboardStorage;
    private readonly IVectorStoreManager<DatabaseSchemaEmbedding> vectorStore;
    private readonly IAIManager aiManager;
    private readonly IUnitOfWork unitOfWork;
    #endregion

    #region Constructor

   public GenerateDashboardCommandHandler(
        UserScopedDashboardStorage dashboardStorage,
        IVectorStoreManager<DatabaseSchemaEmbedding> vectorStore,
        IAIManager aiManager,
        IUnitOfWork unitOfWork)
    {
        this.dashboardStorage = dashboardStorage;
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

        // Generaste SQL Query
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

        var safe = IsSafeSelectQuery(result.Sql);
        if (!safe)
        {
            return Result<string>.CreateFailure([new(Constants.Errors.CannotGenerateDashboard)]);
        }

        var dashboard = CreateDashboard(defaultConnection.Details.SqlServerConfiguration, result.Sql, request.parameters.ChartType, result.Columns ?? []);

        // 7. Return dashboard id to client
        XDocument xdoc = dashboard.SaveToXDocument();

        var dashboardId = $"AI_{Guid.NewGuid():N}";

        // Clen old dashboards from memory
        dashboardStorage.RemoveAllForCurrentUser();

        dashboardStorage.SaveDashboard(dashboardId, xdoc);

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

    private DevExpress.DashboardCommon.Dashboard CreateDashboard(SqlServerConnectionDetailsDto defaultConnection, string sqlQuery, DashboardTypes dashboardType, IEnumerable<string> columns)
    {
        var dashboard = new DevExpress.DashboardCommon.Dashboard();

        var connectionParams = new MsSqlConnectionParameters(
           defaultConnection.InstanceName,
           defaultConnection.DatabaseName,
           defaultConnection.Username,
           defaultConnection.Password,
           MsSqlAuthorizationType.SqlServer
       )
        {
            TrustServerCertificate = DevExpress.Utils.DefaultBoolean.True
        };


        // Create a SQL data source using the AI-generated SQL
        DashboardSqlDataSource sqlDataSource = new DashboardSqlDataSource("DynamicSqlDataSource")
        {
            ConnectionParameters = connectionParams,
        };

        CustomSqlQuery query = new CustomSqlQuery(Constants.Dashboards.DynamicQuery, sqlQuery);
        sqlDataSource.Queries.Add(query);

        // 4. Add data source to dashboard
        dashboard.DataSources.Add(sqlDataSource);

        DashboardItem dashboardItem;

        switch (dashboardType)
        {
            case DashboardTypes.Chart:
                var chart = new ChartDashboardItem
                {
                    ComponentName = "dynamicChart",
                    Name = "AI Generated Chart",
                    DataSource = sqlDataSource,
                    DataMember = Constants.Dashboards.DynamicQuery,
                };

                // X axis: the dimension
                chart.Arguments.Add(new Dimension(Constants.Dashboards.Category));

                // Y axis: the measure via a series
                var value = new Measure(Constants.Dashboards.Value);

                // choose a simple series type; change to Line/Area if you want
                var series = new SimpleSeries(SimpleSeriesType.Bar)
                {
                    Value = value,
                };

                var pane = new ChartPane();
                pane.Series.Add(series);
                chart.Panes.Add(pane);

                dashboardItem = chart;
                break;

            case DashboardTypes.Grid:
                var grid = new GridDashboardItem
                {
                    ComponentName = "dynamicGrid",
                    Name = "AI Generated Grid",
                    DataSource = sqlDataSource,
                    DataMember = Constants.Dashboards.DynamicQuery,
                };

                foreach (var col in columns)
                {
                    grid.Columns.Add(new GridDimensionColumn(new Dimension(col)));
                }

                dashboardItem = grid;
                break;

            case DashboardTypes.Pivot:
                {
                    var pivot = new PivotDashboardItem
                    {
                        ComponentName = "dynamicPivot",
                        Name = "AI Generated Pivot",
                        DataSource = sqlDataSource,
                        DataMember = Constants.Dashboards.DynamicQuery,
                    };

                    if (columns.Count() >= 3)
                    {
                        pivot.Rows.Add(new Dimension(columns.ElementAt(0)));
                        pivot.Columns.Add(new Dimension(columns.ElementAt(1)));
                        pivot.Values.Add(new Measure(columns.ElementAt(2)));
                    }

                    dashboardItem = pivot;
                    break;
                }

            case DashboardTypes.TreeMap:
                {
                    var tree = new TreemapDashboardItem
                    {
                        ComponentName = "dynamicTreeMap",
                        Name = "AI Generated TreeMap",
                        DataSource = sqlDataSource,
                        DataMember = Constants.Dashboards.DynamicQuery,
                    };

                    tree.Arguments.Add(new Dimension(Constants.Dashboards.Category));
                    tree.Values.Add(new Measure(Constants.Dashboards.Value));
                    dashboardItem = tree;
                    break;
                }

            case DashboardTypes.Pie:
            default:
                var pie = new PieDashboardItem
                {
                    ComponentName = "dynamicPieChart",
                    Name = "AI Generated Pie",
                    DataSource = sqlDataSource,
                    DataMember = Constants.Dashboards.DynamicQuery,
                };

                pie.Arguments.Add(new Dimension(Constants.Dashboards.Category));
                pie.Values.Add(new Measure(Constants.Dashboards.Value));
                dashboardItem = pie;
                break;
        }

        dashboard.Items.Add(dashboardItem);

        // 6. Layout
        DashboardLayoutGroup root = new DashboardLayoutGroup();
        root.ChildNodes.Add(new DashboardLayoutItem(dashboardItem) { Weight = 100 });
        dashboard.LayoutRoot = root;

        return dashboard;
    }
    #endregion
}
