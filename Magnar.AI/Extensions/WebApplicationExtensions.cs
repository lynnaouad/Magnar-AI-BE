using Magnar.AI.Application.Configuration;
using Magnar.AI.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Magnar.AI.Extensions;

public static class WebApplicationExtensions
{
    public static void UseCors(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();
        CorsConfiguration corsConfig = scope.ServiceProvider.GetRequiredService<IOptions<CorsConfiguration>>().Value;
        UrlsConfiguration urlConf = scope.ServiceProvider.GetRequiredService<IOptions<UrlsConfiguration>>().Value;

        app.UseCors(builder =>
        {
            builder
            .WithOrigins([.. corsConfig.AllowedOrigins, urlConf.WebUrl ?? string.Empty])
            .AllowAnyHeader()
            .AllowCredentials()
            .AllowAnyMethod()
            .Build();
        });
    }
}
