namespace Magnar.AI.Application.Configuration;

public sealed record CorsConfiguration
{
    public const string SectionName = Constants.Configuration.Sections.Cors;

    public string[] AllowedOrigins { get; init; } = [];
}
