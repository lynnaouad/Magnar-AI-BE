using AutoMapper;
using DevExpress.AspNetCore;
using DevExpress.DashboardAspNetCore;
using DevExpress.DashboardWeb;
using Magnar.AI;
using Magnar.AI.Application;
using Magnar.AI.Application.Dashboards;
using Magnar.AI.Application.Dto.Providers;
using Magnar.AI.Application.Helpers;
using Magnar.AI.Application.Interfaces.Managers;
using Magnar.AI.Application.Interfaces.Repositories;
using Magnar.AI.Domain.Entities;
using Magnar.AI.Domain.Static;
using Magnar.AI.Extensions;
using Magnar.AI.Infrastructure.Extensions;
using Magnar.AI.Infrastructure.Persistence.Contexts;
using Magnar.Recruitment.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using Serilog;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

public partial class Program
{
    private static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.Services.AddHttpClient("cookieClient")
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                return new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (_, _, _, _) => true,
                    UseCookies = true,
                    CookieContainer = new CookieContainer()
                };
            });

        builder.Services.AddHttpClient("fastFailClient")
            .ConfigurePrimaryHttpMessageHandler(() =>
            {
                return new SocketsHttpHandler
                {
                    ConnectTimeout = TimeSpan.FromSeconds(10), // only applies to DNS/TCP connect
                    PooledConnectionLifetime = TimeSpan.FromMinutes(2) // optional
                };
            });

        // Semantic Kernel + OpenAI
        var openAIConf = builder.Configuration.GetSection("OpenAIConfiguration");

        builder.Services.AddKernel()
            .AddOpenAIChatCompletion(openAIConf.GetValue<string>("Model") ?? string.Empty, openAIConf.GetValue<string>("ApiKey") ?? string.Empty)
            .AddOpenAIEmbeddingGenerator(openAIConf.GetValue<string>("EmbeddingModel") ?? string.Empty, openAIConf.GetValue<string>("ApiKey") ?? string.Empty);

        // SQL Vector store
        builder.Services.AddSqlServerVectorStore(
            connectionStringProvider: sp =>
            {
                return sp.GetRequiredService<IConfiguration>().GetConnectionString("default")!;
            },
            lifetime: ServiceLifetime.Singleton);

        var urlsConfig = builder.Configuration.GetSection("UrlsConfiguration");

        var uri = new Uri(urlsConfig.GetValue<string>("Authority"));
        var uniqueDeployName = "Magnar_AI_" + uri.Port;

        builder.Services.AddDataProtection()
                        .PersistKeysToDbContext<MagnarAIDbContext>()
                        .SetApplicationName(uniqueDeployName);

        builder.Services.AddInfrastructureServices(builder.Configuration);

        builder.Services.AddApplicationServices();

        builder.Services.AddWebServices(builder.Configuration, builder.Environment, builder.Host);

        // Register DevExpress dashboard services
        builder.Services.AddSingleton<UserScopedDashboardStorage>();

        builder.Services.AddDevExpressControls();

        builder.Services.AddScoped(sp =>
        {
            var storage = sp.GetRequiredService<UserScopedDashboardStorage>();

            var configurator = new DashboardConfigurator
            {
                AllowExecutingCustomSql = true,
            };

            DashboardConfigurator.PassCredentials = true;
            configurator.SetDashboardStorage(storage);

            configurator.SetConnectionStringsProvider(new DashboardConnectionStringsProvider(builder.Configuration));

            configurator.SetDashboardStorage(storage);

            return configurator;
        });

        WebApplication app = builder.Build();

        /*
         *
         * Configure the HTTP request pipeline.
         *
         */

        if (app.Environment.IsDevelopment())
        {
            app.UseSwaggerUI(s => s.SwaggerEndpoint(
            "/swagger/v1/swagger.json",
            "Magnar AI"));
        }
        else
        {
            app.UseHsts();
        }

        app.UseExceptionHandler();
        await app.InitializeDatabaseAsync();

        // On startup, rebuild all workspace kernels
        using (var scope = app.Services.CreateScope())
        {
            var manager = scope.ServiceProvider.GetRequiredService<IKernelPluginManager>();
            var providerRepository = scope.ServiceProvider.GetRequiredService<IProviderRepository>();
            var workspaceRepository = scope.ServiceProvider.GetRequiredService<IRepository<Workspace>>();
            var mapper = scope.ServiceProvider.GetRequiredService<IMapper>();

            var workspaces = await workspaceRepository.GetAsync();
            var workspacesIds = workspaces.Select(x => x.Id);

            foreach (var workspaceId in workspacesIds)
            {
                var providers = await providerRepository.GetProvidersAsync(x => x.WorkspaceId == workspaceId && x.Type == ProviderTypes.API, default);

                foreach (var provider in providers)
                {
                    var mapped = mapper.Map<ProviderDto>(provider);

                    if (mapped.Details?.ApiProviderAuthDetails is null)
                    {
                        continue;
                    }

                    manager.RebuildKernel(workspaceId, mapped.Id, mapped.ApiProviderDetails, mapped.Details.ApiProviderAuthDetails);
                }
            }
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseSpaStaticFiles();
        app.UseDevExpressControls();

        app.UseRouting();
        app.UseSerilogRequestLogging();
        app.UseRequestLocalization();
        app.UseCors();

        app.UseIdentityServer();
        app.UseAuthentication();
        app.UseAuthorization();

        app.UseSwagger();
        app.UseHealthChecks("/health");

        // DevExpress dashboard endpoint
        app.MapDashboardRoute("api/dashboard", "DefaultDashboard");

        app.MapControllers();

        app.UseSpa((options) => options.Options.SourcePath = Constants.ClientApp.RootPath);

        await app.RunAsync();
    }
}