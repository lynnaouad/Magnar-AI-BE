using DevExpress.DashboardWeb;
using Microsoft.AspNetCore.Http;
using System.Collections.Concurrent;
using System.Xml.Linq;

namespace Magnar.AI.Application.Dashboards;

/// <summary>
/// In-memory dashboard storage scoped by both user and workspace.
/// 
/// Each dashboard is stored under a composite key:
///   {workspaceId}_{username}_{dashboardId}
/// 
/// This ensures that dashboards are isolated:
///   - Per workspace (multi-tenant separation).
///   - Per user (no cross-user sharing inside a workspace).
/// 
/// Note: This storage is ephemeral (lost when the app restarts).
/// </summary>
public class UserScopedDashboardStorage : IDashboardStorage
{
    #region Members
    private readonly IHttpContextAccessor httpContextAccessor;

    // Top-level dictionary:
    //   Key   = full key (workspaceId_username_dashboardId)
    //   Value = dictionary of dashboards for that scope
    private readonly ConcurrentDictionary<string, XDocument> _dashboards = new();
    #endregion

    #region Constructor
    public UserScopedDashboardStorage(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }
    #endregion

    /// <summary>
    /// Returns all dashboards available to the current user in all workspaces.
    /// Extracts dashboards by matching the prefix _{username}_.
    /// </summary>
    public IEnumerable<DashboardInfo> GetAvailableDashboardsInfo()
    {
        var workspaceId = GetWorkspaceId();

        var prefix = $"{workspaceId}_{GetUsername()}";

        var list = _dashboards.Keys
            .Where(k => k.Contains(prefix))
            .Select(k => new DashboardInfo() { ID = k, Name = k });

        return list;
    }

    /// <summary>
    /// Loads a dashboard document by its full key (workspaceId_userId_dashboardId).
    /// Throws an exception if not found for this user/workspace.
    /// </summary>
    public XDocument LoadDashboard(string fullKey)
    {
        if (_dashboards.TryGetValue(fullKey, out var dashboard))
        {
            return dashboard;
        }

        throw new InvalidOperationException("Dashboard not found for this user in this workspace.");
    }

    /// <summary>
    /// Saves (or updates) a dashboard document under its full key (workspaceId_userId_dashboardId). 
    /// </summary>
    public void SaveDashboard(string fullKey, XDocument dashboard)
    {
        _dashboards[fullKey] = dashboard;
    }

    /// <summary>
    /// Removes a specific dashboard under its full key (workspaceId_userId_dashboardId). 
    /// Returns true if removed, false if not found.
    /// </summary>
    public bool RemoveDashboard(string fullKey)
    {
        return _dashboards.TryRemove(fullKey, out _);
    }

    /// <summary>
    /// Removes all dashboards belonging to the current user in the given workspace.
    /// Returns the number of dashboards removed.
    /// </summary>
    public int RemoveAllForCurrentUser(int workspaceId)
    {
        var prefix = GetUserKey(workspaceId) + "_";
        var keys = _dashboards.Keys.Where(k => k.StartsWith(prefix)).ToList();

        int removed = 0;
        foreach (var key in keys)
        {
            if (_dashboards.TryRemove(key, out _))
            {
                removed++;
            }
        }

        return removed; // number of dashboards removed
    }

    /// <summary>
    /// Returns the last (most recently added, alphabetically highest) dashboard key
    /// for the current user in the given workspace.
    /// Returns an empty string if none exist.
    /// </summary>
    public string GetLastDashboardKey(int workspaceId)
    {
        var prefix = GetUserKey(workspaceId) + "_";
        var keys = _dashboards.Keys.Where(k => k.StartsWith(prefix)).ToList();

        if (keys.Count == 0)
        {
            return string.Empty;
        }

        var lastKey = keys.OrderByDescending(k => k).First();

        return lastKey;
    }

    #region Private Methods

    /// <summary>
    /// Builds the user scope key: {workspaceId}_{username}.
    /// Used as the base prefix for all dashboards for this user in this workspace.
    /// </summary>
    private string GetUserKey(int workspaceId)
    {
        var username = GetUsername();

        return $"{workspaceId}_{username}";
    }

    /// <summary>
    /// Gets the username from the current HttpContext user principal.
    /// Falls back to NameIdentifier claim or "Anonymous".
    /// </summary>
    private string GetUsername()
    {
        var user = httpContextAccessor.HttpContext?.User;

        return user?.Identity?.IsAuthenticated == true
            ? user.Identity.Name ?? user.FindFirst(Constants.IdentityApi.ApiClaims.Username)?.Value ?? "Anonymous"
            : "Anonymous";
    }

    private string? GetWorkspaceId()
    {
        return httpContextAccessor.HttpContext?.Request.Headers["X-Workspace-Id"].ToString();
    }
    #endregion
}
