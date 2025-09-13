namespace Magnar.AI.Domain.Static;

public enum ResetToken
{
    Email = 1,
    Password,
    Username,
}

public enum OrderBy
{
    Asc = 1,
    Desc,
}

public enum DashboardTypes
{
    Pie = 1,
    Chart,
    Grid,
    Pivot,
    TreeMap,
}

public enum ProviderTypes
{
    SqlServer = 1,
    API,
}