using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json.Serialization;
using Duende.IdentityServer.Services;
using Magnar.AI.Application.Configuration;
using Magnar.AI.Domain.Entities;
using Magnar.AI.Exceptions;
using Magnar.AI.Infrastructure.Persistence.Contexts;
using Magnar.AI.Infrastructure.Repositories;
using Magnar.Recruitment.Infrastructure.Repositories;
using Magnar.Recruitment.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.OData;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

namespace Magnar.AI;

internal static class DependencyInjection
{
    public static IServiceCollection AddWebServices(
        this IServiceCollection services,
        ConfigurationManager configuration,
        IWebHostEnvironment environment,
        ConfigureHostBuilder host)
    {
        configuration
            .AddConfigurationFiles();

        services
            .BindSystemConfigurations()
            .ConfigureLogging(configuration, host)
            .ConfigureSwagger()
            .ConfigureHealthChecks()
            .ConfigureExceptionHandlers()
            .ConfigureAuthentication(environment)
            .ConfigureApiFeatures();

        return services;
    }

    private static IServiceCollection ConfigureHealthChecks(this IServiceCollection services)
    {
        services
            .AddHealthChecks()
            .AddDbContextCheck<MagnarAIDbContext>();
        return services;
    }

    private static IServiceCollection BindSystemConfigurations(this IServiceCollection services)
    {
        services
            .AddOptions<CorsConfiguration>()
            .BindConfiguration(CorsConfiguration.SectionName)
            .ValidateOnStart()
            .ValidateDataAnnotations();

        services
            .AddOptions<EmailConfiguration>()
            .BindConfiguration(EmailConfiguration.SectionName)
            .ValidateOnStart()
            .ValidateDataAnnotations();

        services
            .AddOptions<IdentityApiConfiguration>()
            .BindConfiguration(IdentityApiConfiguration.SectionName)
            .ValidateOnStart()
            .ValidateDataAnnotations();

        services
            .AddOptions<SwaggerConfiguration>()
            .BindConfiguration(SwaggerConfiguration.SectionName)
            .ValidateOnStart()
            .ValidateDataAnnotations();

        services
            .AddOptions<ReCaptchaConfiguration>()
            .BindConfiguration(ReCaptchaConfiguration.SectionName)
            .ValidateOnStart()
            .ValidateDataAnnotations();

        services
           .AddOptions<ODataConfiguration>()
           .BindConfiguration(ODataConfiguration.SectionName)
           .ValidateOnStart()
           .ValidateDataAnnotations();

        services
           .AddOptions<OpenAIConfiguration>()
           .BindConfiguration(OpenAIConfiguration.SectionName)
           .ValidateOnStart()
           .ValidateDataAnnotations();

        services
           .AddOptions<UrlsConfiguration>()
           .BindConfiguration(UrlsConfiguration.SectionName)
           .ValidateOnStart()
           .ValidateDataAnnotations();

        services
           .AddOptions<VectorConfiguration>()
           .BindConfiguration(VectorConfiguration.SectionName)
           .ValidateOnStart()
           .ValidateDataAnnotations();

        return services;
    }

    private static IServiceCollection ConfigureLogging(this IServiceCollection services, IConfiguration configuration, ConfigureHostBuilder host)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom
            .Configuration(configuration)
            .CreateLogger();

        services.AddSerilog();
        host.UseSerilog();

        return services;
    }

    private static IServiceCollection ConfigureSwagger(this IServiceCollection services)
    {
        using ServiceProvider scope = services.BuildServiceProvider();
        SwaggerConfiguration swaggerConfig = scope.GetRequiredService<IOptions<SwaggerConfiguration>>().Value;

        services.AddSwaggerGen(
            opt =>
            {
                opt.SwaggerDoc(swaggerConfig.Version, new OpenApiInfo
                {
                    Title = swaggerConfig.Title,
                    Description = swaggerConfig.Description,
                    Contact = new OpenApiContact
                    {
                        Name = swaggerConfig.Contact.Name,
                        Email = swaggerConfig.Contact.Email,
                        Url = new Uri(swaggerConfig.Contact.Url ?? string.Empty),
                    },
                });
            }
        );

        return services;
    }

    private static IServiceCollection ConfigureAuthentication(
        this IServiceCollection services, IWebHostEnvironment environment)
    {
        using ServiceProvider scope = services.BuildServiceProvider();
        IdentityApiConfiguration identityConfig = scope.GetRequiredService<IOptions<IdentityApiConfiguration>>().Value;
        UrlsConfiguration urlsConfig = scope.GetRequiredService<IOptions<UrlsConfiguration>>().Value;

        services
            .AddIdentityServer(opt =>
            {
                opt.KeyManagement.Enabled = false;

                // Events
                opt.Events.RaiseErrorEvents = true;
                opt.Events.RaiseInformationEvents = true;
                opt.Events.RaiseFailureEvents = true;
                opt.Events.RaiseSuccessEvents = true;
            })
            .AddSigningCredential(GetSigningCredentials())
            .AddPersistedGrantStore<UserGrantStore>()
            .AddInMemoryClients(identityConfig.Clients)
            .AddInMemoryIdentityResources(IdentityServerStore.GetIdentityResources())
            .AddInMemoryApiResources(IdentityServerStore.GetApiResources())
            .AddInMemoryApiScopes(IdentityServerStore.GetApiScopes())
            .AddAspNetIdentity<ApplicationUser>();

        services.AddTransient<IProfileService, CustomProfileService>();

        services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    LifetimeValidator = (_, expires, _, _) => expires >= DateTime.UtcNow,
                };

                options.RequireHttpsMetadata = !environment.IsDevelopment();
                options.Authority = urlsConfig.Authority;
            });

        return services;
    }

    private static IServiceCollection ConfigureApiFeatures(this IServiceCollection services)
    {
        using ServiceProvider scope = services.BuildServiceProvider();
        ODataConfiguration odataConfig = scope.GetRequiredService<IOptions<ODataConfiguration>>().Value;

        services.AddLocalization(options => options.ResourcesPath = Constants.Localization.DirectoryPath);
        services.Configure<RequestLocalizationOptions>(options =>
        {
            CultureInfo[] supportedCultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
            options.DefaultRequestCulture = new RequestCulture(Constants.Localization.DefaultCulture);
            options.SupportedCultures = supportedCultures;
            options.SupportedUICultures = supportedCultures;
        });

        services.Configure<ApiBehaviorOptions>(config =>
        {
            config.SuppressModelStateInvalidFilter = true;
        });

        services.AddControllers().AddJsonOptions(x =>
        {
            x.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
        })
        .AddOData(options =>
            options.Select()
                   .Filter()
                   .OrderBy()
                   .Expand()
                   .Count()
                   .SetMaxTop(odataConfig.SelectTop));

        services.AddSpaStaticFiles(configuration => configuration.RootPath = Constants.ClientApp.DistPath);

        return services;
    }

    private static IServiceCollection ConfigureExceptionHandlers(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();

        return services;
    }

    private static ConfigurationManager AddConfigurationFiles(this ConfigurationManager configuration)
    {
        configuration.AddJsonFile("Assets/Config/serilog.json", true, true);

        configuration.AddJsonFile("Assets/Config/identityserver.json", true, true);

        return configuration;
    }

    private static SigningCredentials GetSigningCredentials()
    {
        RSA rsaPrivateKey = RSA.Create();
        rsaPrivateKey.ImportPkcs8PrivateKey(Convert.FromBase64String(Constants.SigningCredentials.PrivateKey), out _);

        RsaSecurityKey rsaSecurityKey = new(rsaPrivateKey);

        return new SigningCredentials(rsaSecurityKey, SecurityAlgorithms.RsaSha256);
    }
}
