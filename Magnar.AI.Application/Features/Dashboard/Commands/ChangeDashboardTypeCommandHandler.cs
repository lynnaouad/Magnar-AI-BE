using DevExpress.DashboardCommon;
using Magnar.AI.Application.Dto.Dashboard;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Interfaces.Managers;
using static Magnar.AI.Static.Constants;

namespace Magnar.Recruitment.Application.Features.Dashboard.Commands;

public sealed record ChangeDashboardTypeCommand(DashboardPromptDto parameters) : IRequest<Result<string>>;

public class ChangeDashboardTypeCommandHandler : IRequestHandler<ChangeDashboardTypeCommand, Result<string>>
{
    #region Members
    private readonly IDashboardManager dashboardManager;
    private readonly IUnitOfWork unitOfWork;
    #endregion

    #region Constructor

   public ChangeDashboardTypeCommandHandler(
        IDashboardManager dashboardManager,
        IUnitOfWork unitOfWork)
    {
        this.dashboardManager = dashboardManager;
        this.unitOfWork = unitOfWork;
    }
    #endregion

    public async Task<Result<string>> Handle(ChangeDashboardTypeCommand request, CancellationToken cancellationToken)
    {
        // Check connection
        var defaultConnection = await unitOfWork.ConnectionRepository.GetDefaultConnectionAsync(cancellationToken);
        if(defaultConnection is null)
        {
            return Result<string>.CreateFailure([new(Errors.NoDefaultConnectionConfigured)]);
        }

        if(defaultConnection.Provider != ProviderTypes.SqlServer && defaultConnection?.Details?.SqlServerConfiguration != null)
        {
            return Result<string>.CreateFailure([new(Errors.CannotGenerateDashboard)]);
        }

        var dashboardId = dashboardManager.GetLastDashboardKey();

        if (string.IsNullOrEmpty(dashboardId))
        {
            return Result<string>.CreateSuccess(dashboardId);
        }

        var dashboardXml = dashboardManager.LoadDashboard(dashboardId);

        var dashboard = new DevExpress.DashboardCommon.Dashboard() { };
        dashboard.LoadFromXDocument(dashboardXml);

        // Find the existing item (e.g. pie chart)
        var oldItem = dashboard.Items.FirstOrDefault();
        if(oldItem is null)
        {
            return Result<string>.CreateFailure([Error.None]);
        }

        DashboardSqlDataSource? sqlDataSource = null;
        string? dataMember = null;

        if (oldItem is DataDashboardItem dataItem)
        {
            sqlDataSource = dataItem.DataSource as DashboardSqlDataSource;
            dataMember = dataItem.DataMember;
        }

        // Fallback: grab first registered data source if the old item didn’t expose one
        if (sqlDataSource is null)
        {
            sqlDataSource = dashboard.DataSources.FirstOrDefault() as DashboardSqlDataSource;
            dataMember = sqlDataSource?.Queries.FirstOrDefault()?.Name;
        }

        if (sqlDataSource is null || string.IsNullOrEmpty(dataMember))
        {
            return Result<string>.CreateFailure([new(Constants.Errors.CannotGenerateDashboard)]);
        }

        var dahsboardItem = dashboardManager.CreateDashboardItem(request.parameters.ChartType, sqlDataSource, dataMember, [Constants.Dashboards.Category, Constants.Dashboards.Value]);

        // Replace old item with new one
        dashboard.Items.Remove(oldItem);
        dashboard.Items.Add(dahsboardItem);

        dashboardManager.UpdateDashboardLayout(dashboard, dahsboardItem);

        dashboardManager.SaveDashboard(dashboardId, dashboard.SaveToXDocument());

        return Result<string>.CreateSuccess(dashboardId);
    }
}
