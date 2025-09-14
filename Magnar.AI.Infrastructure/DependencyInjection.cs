using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Interfaces.Managers;
using Magnar.AI.Infrastructure.Interceptors;
using Magnar.AI.Infrastructure.Managers;
using Magnar.AI.Infrastructure.Persistence.Contexts;
using Magnar.AI.Infrastructure.Repositories;
using Magnar.Recruitment.Infrastructure.Managers;
using Magnar.Recruitment.Infrastructure.Repositories;
using Magnar.Recruitment.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;

namespace Magnar.Recruitment.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, ConfigurationManager configuration)
    {
        string connectionString = configuration.GetConnectionString(Constants.Database.DefaultConnectionString);

        ArgumentNullException.ThrowIfNull(connectionString, nameof(connectionString));

        services
            .AddDbContext(connectionString)
            .AddIdentity()
            .AddHybridCache(configuration)
            .RegisterInfrastructureServices();

        return services;
    }

    public static IServiceCollection AddHybridCache(this IServiceCollection services, ConfigurationManager configuration)
    {
        int expiration = configuration.GetValue<int>(Constants.Configuration.Keys.DefaultExpirationInSeconds);

        services.AddHybridCache(OptionsBuilderConfigurationExtensions =>
        {
            OptionsBuilderConfigurationExtensions.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromSeconds(expiration),
                LocalCacheExpiration = TimeSpan.FromSeconds(expiration),
            };
        });

        return services;
    }

    private static IServiceCollection RegisterInfrastructureServices(this IServiceCollection services)
    {
        services.AddScoped<IIdentityRepository, IdentityRepository>();
        services.AddScoped(typeof(IRepository<>), typeof(BaseRepository<>));

        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddScoped<ISchemaManager, SchemaManager>();

        services.AddScoped(typeof(IVectorStoreManager<>), typeof(VectorStoreManager<>));

        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IReCaptchaService, ReCaptchaService>();

        return services;
    }

    private static IServiceCollection AddIdentity(this IServiceCollection services)
    {
        services
            .AddIdentity<ApplicationUser, IdentityRole<int>>(options =>
            {
                options.Password.RequiredLength = 8;
                options.Password.RequiredUniqueChars = 1;

                options.User.RequireUniqueEmail = true;

                options.Stores.MaxLengthForKeys = 128;

                options.SignIn.RequireConfirmedAccount = true;
            })
            .AddEntityFrameworkStores<MagnarAIDbContext>()
            .AddDefaultTokenProviders();
        return services;
    }

    private static IServiceCollection AddDbContext(this IServiceCollection services, string connectionString)
    {
        services.AddScoped<AuditableEntityInterceptor>();

        services.AddDbContext<MagnarAIDbContext>(
            config =>
            {
                config.UseSqlServer(connectionString);
            });

        services.AddScoped<DatabaseInitializer>();

        return services;
    }
}
