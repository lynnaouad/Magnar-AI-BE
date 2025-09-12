using System.ComponentModel.DataAnnotations;

namespace Magnar.AI.Application.Configuration;

public sealed record OpenAIConfiguration
{
    public const string SectionName = Constants.Configuration.Sections.OpenAIConfiguration;

    [Required]
    public bool Enabled { get; init; } = false;

    public string? ApiKey { get; init; }

    [Required]
    public string Model { get; init; } = string.Empty;

    [Required]
    public string EmbeddingModel { get; init; } = string.Empty;
}
