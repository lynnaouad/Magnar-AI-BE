using DevExpress.DashboardCommon;
using Magnar.AI.Application.Dto.Providers;
using System.Xml.Linq;

namespace Magnar.AI.Application.Interfaces.Managers
{
    public interface IDashboardManager
    {
        Dashboard CreateDashboard(SqlServerProviderDetailsDto defaultConnection, string sqlQuery, DashboardTypes dashboardType, IEnumerable<string> columns);

        DashboardSqlDataSource CreateSqlDatasource(SqlServerProviderDetailsDto defaultConnection, string sqlQuery);

        DashboardItem CreateDashboardItem(DashboardTypes dashboardType, DashboardSqlDataSource sqlDataSource, string dataMember = Constants.Dashboards.DynamicQuery, IEnumerable<string>? columns = null);

        void UpdateDashboardLayout(Dashboard dashboard, DashboardItem dashboardItem);

        void RemoveAllForCurrentUser(int workspaceId);

        string SaveDashboard(int workspaceId, string username, string dashboardId, XDocument xdoc);

        string SaveDashboard(string fullkey, XDocument xdoc);

        string GetLastDashboardKey(int workspaceId);

        XDocument LoadDashboard(string fullKey);
    }
}
