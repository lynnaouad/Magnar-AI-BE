using Magnar.AI.Application.Behaviors;
using Magnar.AI.Application.Interfaces.Managers;
using Magnar.AI.Application.Kernel;
using Magnar.AI.Application.Managers;
using Magnar.AI.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Reflection;

namespace Magnar.AI.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services
            .AddFluentValidation()
            .AddAutoMapper()
            .RegisterApplicationServices()
            .AddMediatR();

        return services;
    }

    public static IServiceCollection AddFluentValidation(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }

    public static IServiceCollection AddAutoMapper(this IServiceCollection services)
    {
        services.AddAutoMapper(Assembly.GetExecutingAssembly());

        return services;
    }

    public static IServiceCollection AddMediatR(this IServiceCollection services)
    {
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        return services;
    }

    private static IServiceCollection RegisterApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthorizationService, AuthorizationService>();

        services.AddScoped<IAIManager, AIManager>();
        services.AddScoped<IDashboardManager, DashboardManager>();

        services.AddSingleton<IKernelPluginManager, KernelPluginManager>();
        services.AddScoped<IKernelPluginService, KernelPluginService>();

        return services;
    }
}
