using System.Reflection;
using Magnar.AI.Application.Behaviors;
using Magnar.AI.Application.Interfaces.Managers;
using Magnar.AI.Application.Managers;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddScoped<IAIManager, AIManager>();
        services.AddScoped<IAnnotationFileManager, AnnotationFileManager>();

        return services;
    }
}
