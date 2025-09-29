using Magnar.AI.Application.Dto.AI.SemanticSearch;
using Magnar.AI.Application.Dto.Dashboard;
using Magnar.AI.Application.Features.DatabaseSchema.Commands;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Interfaces.Managers;
using Microsoft.AspNetCore.Http;
using Serilog;
using System.Text.Json;
using System.Xml.Linq;

namespace Magnar.Recruitment.Application.Features.Dashboard.Commands;

public sealed record GenerateDashboardCommand(DashboardPromptDto Parameters) : IRequest<Result<string>>;

public class GenerateDashboardCommandHandler : IRequestHandler<GenerateDashboardCommand, Result<string>>
{
    #region Members
    private readonly IDashboardManager dashboardManager;
    private readonly IUnitOfWork unitOfWork;
    private readonly ICurrentUserService currentUserService;
    private readonly IMediator mediator;
    private readonly IAuthorizationService authorizationService;
    #endregion

    #region Constructor

    public GenerateDashboardCommandHandler(
        IDashboardManager dashboardManager,
        ICurrentUserService currentUserService,
        IAuthorizationService authorizationService,
        IMediator mediator,
        IUnitOfWork unitOfWork)
    {
        this.dashboardManager = dashboardManager;
        this.unitOfWork = unitOfWork;
        this.currentUserService = currentUserService;
        this.mediator = mediator;
        this.authorizationService = authorizationService;
    }
    #endregion

    public async Task<Result<string>> Handle(GenerateDashboardCommand request, CancellationToken cancellationToken)
    {
        // Authorization check
        var canAccessWorkspace = await authorizationService.CanAccessWorkspace(request.Parameters.WorkspaceId, cancellationToken);
        if (!canAccessWorkspace)
        {
            return Result<string>.CreateFailure([new(Constants.Errors.Unauthorized)], StatusCodes.Status401Unauthorized);
        }

        var result = await mediator.Send(new GenerateAndExecuteSqlQueryCommand(request.Parameters.Prompt, request.Parameters.WorkspaceId, false, request.Parameters.ChartType, request.Parameters.ProviderId), default);
        if (!result.Success)
        {
            Log.Error(string.Join(',', result.Errors?.Select(x => x.Message) ?? []));
            return Result<string>.CreateFailure([new(Constants.Errors.CannotGenerateDashboard)]);
        }

        var provider = await unitOfWork.ProviderRepository.GetProviderAsync(request.Parameters.ProviderId, cancellationToken);
        if(provider is null || provider.Details?.SqlServerConfiguration is null)
        {
            return Result<string>.CreateFailure([new(Constants.Errors.NoDefaultConnectionConfigured)]);
        }

        var aiResult = JsonSerializer.Deserialize<DatabaseSchemaSqlDto>(result.Value) ?? new DatabaseSchemaSqlDto();

        var dashboard = dashboardManager.CreateDashboard(provider.Details.SqlServerConfiguration, aiResult.Sql, request.Parameters.ChartType, aiResult.Columns);

        XDocument xdoc = dashboard.SaveToXDocument();

        var dashboardId = $"AI_{Guid.NewGuid():N}";

        // Clean old dashboards from memory
        dashboardManager.RemoveAllForCurrentUser(request.Parameters.WorkspaceId);

        var fullkey = dashboardManager.SaveDashboard(request.Parameters.WorkspaceId, currentUserService.GetUsername(), dashboardId, xdoc);

        return Result<string>.CreateSuccess(fullkey);
    }
}
