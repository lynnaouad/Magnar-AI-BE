using DevExpress.DashboardCommon;
using Magnar.AI.Application.Dto.Dashboard;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Interfaces.Managers;
using Microsoft.AspNetCore.Http;

namespace Magnar.Recruitment.Application.Features.Dashboard.Commands;

public sealed record ChangeDashboardTypeCommand(DashboardPromptDto Parameters) : IRequest<Result<string>>;

public class ChangeDashboardTypeCommandHandler : IRequestHandler<ChangeDashboardTypeCommand, Result<string>>
{
    #region Members
    private readonly IDashboardManager dashboardManager;
    private readonly IUnitOfWork unitOfWork;
    private readonly ICurrentUserService currentUserService;
    private readonly IAuthorizationService authorizationService;
    #endregion

    #region Constructor

    public ChangeDashboardTypeCommandHandler(
        IDashboardManager dashboardManager,
        ICurrentUserService currentUserService,
        IAuthorizationService authorizationService,
        IUnitOfWork unitOfWork)
    {
        this.dashboardManager = dashboardManager;
        this.unitOfWork = unitOfWork;
        this.currentUserService = currentUserService;
        this.authorizationService = authorizationService;
    }
    #endregion

    public async Task<Result<string>> Handle(ChangeDashboardTypeCommand request, CancellationToken cancellationToken)
    {
        var canAccessWorkspace = await authorizationService.CanAccessWorkspace(request.Parameters.WorkspaceId, cancellationToken);
        if (!canAccessWorkspace )
        {
            return Result<string>.CreateFailure([new(Constants.Errors.Unauthorized)], StatusCodes.Status401Unauthorized);
        }

        var dashboardId = dashboardManager.GetLastDashboardKey(request.Parameters.WorkspaceId);

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

        var dahsboardItem = dashboardManager.CreateDashboardItem(request.Parameters.ChartType, sqlDataSource, dataMember, [Constants.Dashboards.Category, Constants.Dashboards.Value]);

        // Replace old item with new one
        dashboard.Items.Remove(oldItem);
        dashboard.Items.Add(dahsboardItem);

        dashboardManager.UpdateDashboardLayout(dashboard, dahsboardItem);

        dashboardManager.SaveDashboard(dashboardId, dashboard.SaveToXDocument());

        return Result<string>.CreateSuccess(dashboardId);
    }
}
