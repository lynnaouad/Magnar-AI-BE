using AutoMapper;
using Magnar.AI.Application.Dto.Providers;
using Magnar.AI.Application.Helpers;
using Magnar.AI.Domain.Entities;
using Magnar.AI.Domain.Static;
using Magnar.AI.Infrastructure.Persistence.Contexts;
using Microsoft.AspNetCore.DataProtection;
using System.Data.SqlClient;
using System.Linq.Expressions;
using System.Threading;

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
        var provider = await context
            .Set<Provider>()
            .Include(provider => provider.ApiProviderDetails)
            .FirstOrDefaultAsync(x => x.WorkspaceId == workspaceId && x.IsDefault && x.Type == providerType, cancellationToken: cancellationToken);
       
        if (provider is null)
        {
            return null;
        }

        var mappedProvider = mapper.Map<ProviderDto>(provider);

        UnprotectProvider(mappedProvider);

        return mappedProvider;
    }

    public async Task<ProviderDto> GetProviderAsync(int id, CancellationToken cancellationToken)
    {
        var provider = await context
            .Set<Provider>()
            .Include(provider => provider.ApiProviderDetails)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken: cancellationToken);

        if (provider is null || string.IsNullOrEmpty(provider.Details))
        {
            return null;
        }

        var mappedProvider = mapper.Map<ProviderDto>(provider);

        UnprotectProvider(mappedProvider);

        return mappedProvider;
    }

    public async Task<IEnumerable<Provider>> GetProvidersAsync(Expression<Func<Provider, bool>> filter, CancellationToken cancellationToken)
    {
        var providers = await context
            .Set<Provider>()
            .Include(provider => provider.ApiProviderDetails)
            .Where(filter)
            .ToListAsync(cancellationToken);

        if (providers is null || providers.Count == 0)
        {
            return [];
        }

        return providers;
    }

    public async Task<int> CreateProviderAsync(ProviderDto provider, CancellationToken cancellationToken)
    {
        ProtectProvider(provider);

        var toCreate = mapper.Map<Provider>(provider);

        await context.Set<Provider>().AddAsync(toCreate, cancellationToken);

        await context.SaveChangesAsync(cancellationToken);

        return toCreate.Id;
    }

    public async Task UpdateProviderAsync(ProviderDto provider, CancellationToken cancellationToken)
    {
        ProtectProvider(provider);

        context.Set<Provider>().Update(mapper.Map<Provider>(provider));

        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> TestSqlProviderAsync(SqlServerProviderDetailsDto details, CancellationToken cancellationToken)
    {
        if(details is null)
        {
            return false;
        }

        var connectionString = Utilities.BuildSqlServerConnectionString(details);

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

    #region Private Methods

    private void ProtectProvider(ProviderDto provider) => TransformProviderSecrets(provider, Protect);
   
    private void UnprotectProvider(ProviderDto provider) => TransformProviderSecrets(provider, Unprotect);

    private void TransformProviderSecrets(ProviderDto provider, Func<string, string> transformer)
    {
        switch (provider.Type)
        {
            case ProviderTypes.SqlServer:
                if (!string.IsNullOrEmpty(provider.Details?.SqlServerConfiguration?.Password))
                {
                    provider.Details.SqlServerConfiguration.Password = transformer(provider.Details.SqlServerConfiguration.Password);
                }
                break;

            case ProviderTypes.API:
                var auth = provider.Details?.ApiProviderAuthDetails;
                if (auth is null)
                {
                    break;
                }

                auth.ClientId = Transform(auth.ClientId, transformer);
                auth.ClientSecret = Transform(auth.ClientSecret, transformer);
                auth.Username = Transform(auth.Username, transformer);
                auth.Password = Transform(auth.Password, transformer);
                auth.ApiKeyValue = Transform(auth.ApiKeyValue, transformer);

                break;
        }
    }

    private string Transform(string field, Func<string, string> transformer)
    {
        if (!string.IsNullOrEmpty(field))
        {
            return transformer(field);
        }

        return field;
    }

    private string Protect(string value)
    {
        return protector.Protect(value);
    }

    private string Unprotect(string protectedValue)
    {
        return protector.Unprotect(protectedValue);
    }

    #endregion
}
