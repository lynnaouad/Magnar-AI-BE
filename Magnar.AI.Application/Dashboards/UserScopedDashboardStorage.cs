using System.Collections.Concurrent;
using System.IdentityModel.Claims;
using System.Xml.Linq;
using DevExpress.DashboardWeb;
using Microsoft.AspNetCore.Http;

namespace Magnar.AI.Application.Dashboards;

public class UserScopedDashboardStorage : IDashboardStorage
{
    #region Members
    private readonly IHttpContextAccessor httpContextAccessor;

    private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, XDocument>> _dashboards
        = new();
    #endregion

    #region Constructor
    public UserScopedDashboardStorage(IHttpContextAccessor httpContextAccessor)
    {
        this.httpContextAccessor = httpContextAccessor;
    }
    #endregion

    public IEnumerable<DashboardInfo> GetAvailableDashboardsInfo()
    {
        var prefix = GetUserKey() + "_";

        return _dashboards.Keys
            .Where(k => k.StartsWith(prefix))
            .Select(k => new DashboardInfo() { ID = k.Substring(prefix.Length), Name = k.Substring(prefix.Length) });
    }

    public XDocument LoadDashboard(string dashboardID)
    {
        var fullId = GetFullId(dashboardID);

        if (_dashboards.TryGetValue(fullId, out var dashboardsForUser))
        {
            if (dashboardsForUser.TryGetValue(dashboardID, out var doc))
            {
                return doc;
            }
        }

        throw new InvalidOperationException("Dashboard not found for this user.");
    }

    public void SaveDashboard(string dashboardID, XDocument dashboard)
    {
        var fullId = GetFullId(dashboardID);
        var dashboardsForUser = _dashboards.GetOrAdd(fullId, _ => new ConcurrentDictionary<string, XDocument>());
        dashboardsForUser[dashboardID] = dashboard;
    }

    public bool RemoveDashboard(string dashboardID)
    {
        var fullId = GetFullId(dashboardID);
        if (_dashboards.TryGetValue(fullId, out var dashboardsForUser))
        {
            return dashboardsForUser.TryRemove(dashboardID, out _);
        }

        return false;
    }

    public int RemoveAllForCurrentUser()
    {
        var prefix = GetUserKey() + "_";
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

    #region Private Methods

    private string GetUserKey()
    {
        var user = httpContextAccessor.HttpContext?.User;
        return user?.Identity?.IsAuthenticated == true
            ? user.Identity.Name ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "Anonymous"
            : "Anonymous";
    }

    private string GetFullId(string dashboardID)
    {
        return $"{GetUserKey()}_{dashboardID}";
    }
    #endregion
}
