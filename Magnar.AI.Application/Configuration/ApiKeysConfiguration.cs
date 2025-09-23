using System.ComponentModel.DataAnnotations;

namespace Magnar.AI.Application.Configuration;

public sealed record ApiKeysConfiguration
{
    public const string SectionName = Constants.Configuration.Sections.ApiKeysConfiguration;

    [Required]
    public string HashSecret { get; init; } = string.Empty;

}