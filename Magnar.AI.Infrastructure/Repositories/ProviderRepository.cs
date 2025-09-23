using AutoMapper;
using Magnar.AI.Application.Dto.Providers;
using Magnar.AI.Application.Models.Responses;
using Magnar.AI.Domain.Entities;
using Magnar.AI.Domain.Static;
using Magnar.AI.Infrastructure.Persistence.Contexts;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.OData.Query;
using System.Data.SqlClient;

namespace Magnar.AI.Infrastructure.Repositories;

public class ProviderRepository : BaseRepository<Provider>, IProviderRepository
{
    #region Members
    private readonly MagnarAIDbContext context;
    private readonly IMapper mapper;
    private readonly IDataProtector protector;
    private IRepository<ApiProviderDetails> apiProviderDetailsRepository;
    #endregion

    #region Constructor
    public ProviderRepository(MagnarAIDbContext context, IMapper mapper, IDataProtectionProvider dataProtectorProvider, IRepository<ApiProviderDetails> apiProviderDetailsRepository) : base(context)
    {
        this.context = context;
        this.mapper = mapper;
        protector = dataProtectorProvider.CreateProtector(Constants.DataProtector.Purpose);
        this.apiProviderDetailsRepository = apiProviderDetailsRepository;
    }
    #endregion

    public IRepository<ApiProviderDetails> ApiProviderDetailsRepository => apiProviderDetailsRepository;

    public async Task<ProviderDto> GetDefaultProviderAsync(int workspaceId, ProviderTypes providerType, CancellationToken cancellationToken)
    {
        var provider = await context.Set<Provider>().FirstOrDefaultAsync(x => x.WorkspaceId == workspaceId && x.IsDefault && x.Type == providerType, cancellationToken: cancellationToken);
        if (provider is null)
        {
            return null;
        }

        return mapper.Map<ProviderDto>(provider);
    }

    public async Task<ProviderDto> GetProviderAsync(int id, CancellationToken cancellationToken)
    {
        var provider = await context.Set<Provider>().FirstOrDefaultAsync(x => x.Id == id, cancellationToken: cancellationToken);
        if (provider is null || string.IsNullOrEmpty(provider.Details))
        {
            return null;
        }

        var mappedProvider = mapper.Map<ProviderDto>(provider);

        switch (provider.Type) 
        {
            case ProviderTypes.SqlServer:
                {
                    break;
                }

            case ProviderTypes.API:
                {
                    var apiDetails = context.Set<ApiProviderDetails>().Where(x => x.ProviderId == provider.Id);

                    mappedProvider.ApiProviderDetails = mapper.Map<IEnumerable<ApiProviderDetailsDto>>(apiDetails);
                    break;
                }

            default: break;
        }

        return mappedProvider;
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

    #endregion
}
