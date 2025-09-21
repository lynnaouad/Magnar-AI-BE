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
    TreeMap,
}

public enum ProviderTypes
{
    SqlServer = 1,
    API
}

public enum ApiParameterLocation
{
    Query = 1,
    Route
}

public enum ApiParameterDataType
{
    String = 1,
    Integer, 
    Double,
    Float,
    Decimal,
    Boolean,  
    DateTime,
    DateTimeOffset,
    Guid,
    Enum,
    Array,
}

public enum HttpMethods
{
    GET = 1,
    POST,
    PUT,
    PATCH,
    DELETE
}

public enum AuthType 
{
    None = 1,
    PasswordCredentials,
    ClientCredentials
}
