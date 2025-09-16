using DevExpress.DashboardCommon;
using DevExpress.DataAccess.ConnectionParameters;
using DevExpress.DataAccess.Sql;
using Magnar.AI.Application.Dashboards;
using Magnar.AI.Application.Dto.Connection;
using Magnar.AI.Application.Interfaces.Managers;
using System.Xml.Linq;

namespace Magnar.AI.Application.Managers
{
    public class DashboardManager : IDashboardManager
    {
        public readonly UserScopedDashboardStorage dashboardStorage;

        public DashboardManager(UserScopedDashboardStorage dashboardStorage)
        {
            this.dashboardStorage = dashboardStorage;
        }

        public Dashboard CreateDashboard(SqlServerConnectionDetailsDto defaultConnection, string sqlQuery, DashboardTypes dashboardType, IEnumerable<string>? columns = null)
        {
            var dashboard = new Dashboard();

            // Create a SQL data source using the AI-generated SQL
            DashboardSqlDataSource sqlDataSource = CreateSqlDatasource(defaultConnection, sqlQuery);

            // Add data source to dashboard
            dashboard.DataSources.Add(sqlDataSource);

            DashboardItem dashboardItem = CreateDashboardItem(dashboardType, sqlDataSource, columns: columns);

            dashboard.Items.Add(dashboardItem);

            UpdateDashboardLayout(dashboard, dashboardItem);

            return dashboard;
        }
    
        public DashboardSqlDataSource CreateSqlDatasource(SqlServerConnectionDetailsDto defaultConnection, string sqlQuery)
        {
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
            DashboardSqlDataSource sqlDataSource = new("DynamicSqlDataSource")
            {
                ConnectionParameters = connectionParams,
            };

            CustomSqlQuery query = new(Constants.Dashboards.DynamicQuery, sqlQuery);
            sqlDataSource.Queries.Add(query);

            return sqlDataSource;
        }
    
        public DashboardItem CreateDashboardItem(DashboardTypes dashboardType, DashboardSqlDataSource sqlDataSource, string dataMember = Constants.Dashboards.DynamicQuery, IEnumerable<string>? columns = null)
        {
            DashboardItem dashboardItem;

            switch (dashboardType)
            {
                case DashboardTypes.Chart:
                    var chart = new ChartDashboardItem
                    {
                        ComponentName = "dynamicChart",
                        Name = "AI Generated Chart",
                        DataSource = sqlDataSource,
                        DataMember = dataMember,
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
                        DataMember = dataMember,
                    };

                    columns ??= [];

                    foreach (var col in columns)
                    {
                        grid.Columns.Add(new GridDimensionColumn(new Dimension(col)));
                    }

                    dashboardItem = grid;
                    break;

                case DashboardTypes.TreeMap:
                    {
                        var tree = new TreemapDashboardItem
                        {
                            ComponentName = "dynamicTreeMap",
                            Name = "AI Generated TreeMap",
                            DataSource = sqlDataSource,
                            DataMember = dataMember,
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
                        DataMember = dataMember,
                    };

                    pie.Arguments.Add(new Dimension(Constants.Dashboards.Category));
                    pie.Values.Add(new Measure(Constants.Dashboards.Value));
                    dashboardItem = pie;
                    break;
            }

            return dashboardItem;
        }
    
        public void UpdateDashboardLayout(Dashboard dashboard, DashboardItem dashboardItem)
        {
            DashboardLayoutGroup root = new DashboardLayoutGroup();
            root.ChildNodes.Add(new DashboardLayoutItem(dashboardItem) { Weight = 100 });
            dashboard.LayoutRoot = root;
        }

        public void RemoveAllForCurrentUser()
        {
            dashboardStorage.RemoveAllForCurrentUser();
        }

        public void SaveDashboard(string dashboardId, XDocument xdoc)
        {
            dashboardStorage.SaveDashboard(dashboardId, xdoc);
        }

        public string GetLastDashboardKey()
        {
            return dashboardStorage.GetLastDashboardKey();
        }

        public XDocument LoadDashboard(string dashboardID)
        {
            return dashboardStorage.LoadDashboard(dashboardID);
        }
    }
}
