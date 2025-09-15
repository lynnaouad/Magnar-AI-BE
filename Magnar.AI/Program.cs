using DevExpress.AspNetCore;
using DevExpress.DashboardAspNetCore;
using DevExpress.DashboardWeb;
using DevExpress.DataAccess.ConnectionParameters;
using Magnar.AI;
using Magnar.AI.Application;
using Magnar.AI.Application.Dashboards;
using Magnar.AI.Extensions;
using Magnar.AI.Infrastructure.Extensions;
using Magnar.Recruitment.Infrastructure;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.SemanticKernel;
using Serilog;
using System.Threading.Tasks;

public partial class Program
{
    private static async Task Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

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

        builder.Services.AddDataProtection();

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

            //configurator.ConfigureDataConnection += (s, e) =>
            //{
            //    if (e.ConnectionName == "Dynamic") // must match the ConnectionName you set in GenerateDashboard
            //    {
            //        e.ConnectionParameters = new MsSqlConnectionParameters(
            //            "LYNN\\SQL2025",
            //            "RecruitmentDbTest",
            //            "sa",
            //            "lynnaouad",
            //            MsSqlAuthorizationType.SqlServer
            //        );
            //    }
            //};

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