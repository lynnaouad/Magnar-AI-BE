using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Magnar.AI.Infrastructure.Extensions;

public static class InfrastructureExtensions
{
    public static async Task<WebApplication> InitializeDatabaseAsync(this WebApplication app)
    {
        using IServiceScope scope = app.Services.CreateScope();
        DatabaseInitializer initializer = scope.ServiceProvider.GetRequiredService<DatabaseInitializer>();

        await initializer.InitializeAsync();

        await initializer.SeedAsync();

        return app;
    }

    /// <summary>
    /// Deserializes a JSON string into an object of the specified type.
    /// </summary>
    /// <typeparam name="TResponse">The type of the object to deserialize to.</typeparam>
    /// <param name="contentString">The JSON string to be deserialized.</param>
    /// <returns>
    /// An object of type <typeparamref name="TResponse"/> if deserialization is successful.
    /// otherwise, the default value of <typeparamref name="TResponse"/> (e.g., null for reference types).
    /// </returns>
    public static TResponse DeserializeJsonString<TResponse>(this string contentString)
        where TResponse : class
    {
        TResponse response = default;
        try
        {
            response = JsonSerializer.Deserialize<TResponse>(contentString);
        }
        catch (Exception ex)
        {
            Log.Logger.Error(ex, ex.Message);
            return response;
        }

        return response;
    }
}
