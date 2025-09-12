using System.ComponentModel.DataAnnotations;

namespace Magnar.AI.Application.Configuration;

public sealed record ReCaptchaConfiguration
{
    public const string SectionName = Constants.Configuration.Sections.ReCaptcha;

    [Required]
    public string SiteKey { get; init; } = string.Empty;

    [Required]
    public string SecretKey { get; init; } = string.Empty;

    [Required]
    public string Url { get; init; } = string.Empty;
}