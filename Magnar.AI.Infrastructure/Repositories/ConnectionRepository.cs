using AutoMapper;
using Magnar.AI.Application.Dto.Connection;
using Magnar.AI.Application.Models.Responses;
using Magnar.AI.Domain.Static;
using Magnar.AI.Infrastructure.Persistence.Contexts;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.OData.Query;
using System.Data.SqlClient;

namespace Magnar.AI.Infrastructure.Repositories;

public class ConnectionRepository : BaseRepository<Connection>, IConnectionRepository
{
    #region Members
    private readonly DbSet<Connection> context;
    private readonly IMapper mapper;
    private readonly IDataProtector protector;
    #endregion

    #region Constructor
    public ConnectionRepository(MagnarAIDbContext context, IMapper mapper, IDataProtectionProvider dataProtectorProvider) : base(context)
    {
        this.context = context.Set<Connection>();
        this.mapper = mapper;
        protector = dataProtectorProvider.CreateProtector(Constants.DataProtector.Purpose);
    }
    #endregion

    public async Task<ConnectionDto> GetDefaultConnectionAsync(CancellationToken cancellationToken)
    {
        var defaultConnection = await context.FirstOrDefaultAsync(x => x.IsDefault, cancellationToken);
        if (defaultConnection is null)
        {
            return null;
        }

        var mappedConnection = mapper.Map<ConnectionDto>(defaultConnection);

        return HandlePasswords(mappedConnection);
    }

    public async Task<ConnectionDto> GetConnectionAsync(int id, CancellationToken cancellationToken)
    {
        var connection = await context.FirstOrDefaultAsync(x => x.Id == id, cancellationToken: cancellationToken);
        if (connection is null || string.IsNullOrEmpty(connection.Details))
        {
            return null;
        }

        var mappedConnection = mapper.Map<ConnectionDto>(connection);

        return HandlePasswords(mappedConnection);
    }

    public async Task<bool> TestSqlConnectionAsync(SqlServerConnectionDetailsDto details, CancellationToken cancellationToken)
    {
        if(details is null)
        {
            return false;
        }

        var connectionString = BuildSqlServerConnectionString(details);

        try
        {
            await using var conn = new SqlConnection(connectionString);
            await conn.OpenAsync(cancellationToken);

            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public async Task<OdataResponse<ConnectionDto>> GetConnectionsOdataAsync(ODataQueryOptions<Connection> filterOptions, CancellationToken cancellationToken)
    {
        var result = await OdataGetAsync(filterOptions, cancellationToken: cancellationToken);

        var mappedConnections = mapper.Map<IEnumerable<ConnectionDto>>(result.Value);

        mappedConnections = mappedConnections.Select(x =>
        {
            return HandlePasswords(x);
        });

        return new OdataResponse<ConnectionDto>
        {
            TotalCount = result.TotalCount,
            Value = mappedConnections
        };
    }

    public string BuildSqlServerConnectionString(SqlServerConnectionDetailsDto details)
    {
        if(details is null)
        {
            return string.Empty;
        }

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = details.InstanceName,
            InitialCatalog = details.DatabaseName,
            UserID = details.Username,
            Password = details.Password,
            TrustServerCertificate = true,
            MultipleActiveResultSets = true,
            ConnectTimeout = 30
        };

        return builder.ConnectionString;
    }

    public string ProtectPassword(string password)
    {
        return protector.Protect(password);
    }

    public string UnprotectPassword(string protectedPassword)
    {
        return protector.Unprotect(protectedPassword);
    }

    #region Private Methods
    private ConnectionDto HandlePasswords(ConnectionDto connection)
    {
        if (connection.Provider == ProviderTypes.SqlServer && connection.Details?.SqlServerConfiguration is not null)
        {
            connection.Details.SqlServerConfiguration.Password = UnprotectPassword(connection.Details.SqlServerConfiguration.Password);
        }

        return connection;
    }
    #endregion
}
