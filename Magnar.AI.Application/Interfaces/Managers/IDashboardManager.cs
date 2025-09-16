using DevExpress.DashboardCommon;
using Magnar.AI.Application.Dto.Connection;
using System.Xml.Linq;

namespace Magnar.AI.Application.Interfaces.Managers
{
    public interface IDashboardManager
    {
        Dashboard CreateDashboard(SqlServerConnectionDetailsDto defaultConnection, string sqlQuery, DashboardTypes dashboardType, IEnumerable<string> columns);

        DashboardSqlDataSource CreateSqlDatasource(SqlServerConnectionDetailsDto defaultConnection, string sqlQuery);

        DashboardItem CreateDashboardItem(DashboardTypes dashboardType, DashboardSqlDataSource sqlDataSource, string dataMember = Constants.Dashboards.DynamicQuery, IEnumerable<string>? columns = null);

        void UpdateDashboardLayout(Dashboard dashboard, DashboardItem dashboardItem);

        void RemoveAllForCurrentUser();

        void SaveDashboard(string dashboardId, XDocument xdoc);

        string GetLastDashboardKey();

        XDocument LoadDashboard(string dashboardID);
    }
}
