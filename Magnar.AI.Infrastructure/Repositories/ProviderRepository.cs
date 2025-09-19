using AutoMapper;
using Magnar.AI.Application.Dto.Providers;
using Magnar.AI.Application.Models.Responses;
using Magnar.AI.Domain.Static;
using Magnar.AI.Infrastructure.Persistence.Contexts;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.OData.Query;
using System.Data.SqlClient;

namespace Magnar.AI.Infrastructure.Repositories;

public class ProviderRepository : BaseRepository<Provider>, IProviderRepository
{
    #region Members
    private readonly DbSet<Provider> context;
    private readonly IMapper mapper;
    private readonly IDataProtector protector;
    #endregion

    #region Constructor
    public ProviderRepository(MagnarAIDbContext context, IMapper mapper, IDataProtectionProvider dataProtectorProvider) : base(context)
    {
        this.context = context.Set<Provider>();
        this.mapper = mapper;
        protector = dataProtectorProvider.CreateProtector(Constants.DataProtector.Purpose);
    }
    #endregion

    public async Task<ProviderDto> GetProviderAsync(int id, CancellationToken cancellationToken)
    {
        var connection = await context.FirstOrDefaultAsync(x => x.Id == id, cancellationToken: cancellationToken);
        if (connection is null || string.IsNullOrEmpty(connection.Details))
        {
            return null;
        }

        var mappedProvider = mapper.Map<ProviderDto>(connection);

        return HandlePasswords(mappedProvider);
    }

    public async Task<bool> TestSqlProviderAsync(SqlServerProviderDetailsDto details, CancellationToken cancellationToken)
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

    public async Task<OdataResponse<ProviderDto>> GetProvidersOdataAsync(ODataQueryOptions<Provider> filterOptions, CancellationToken cancellationToken)
    {
        var result = await OdataGetAsync(filterOptions, cancellationToken: cancellationToken);

        var mappedProviders = mapper.Map<IEnumerable<ProviderDto>>(result.Value);

        mappedProviders = mappedProviders.Select(HandlePasswords);

        return new OdataResponse<ProviderDto>
        {
            TotalCount = result.TotalCount,
            Value = mappedProviders
        };
    }

    public string BuildSqlServerConnectionString(SqlServerProviderDetailsDto details)
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
            ConnectTimeout = 30,
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
    private ProviderDto HandlePasswords(ProviderDto connection)
    {
        if (connection.Type == ProviderTypes.SqlServer && connection.Details?.SqlServerConfiguration is not null)
        {
            connection.Details.SqlServerConfiguration.Password = UnprotectPassword(connection.Details.SqlServerConfiguration.Password);
        }

        return connection;
    }
    #endregion
}
