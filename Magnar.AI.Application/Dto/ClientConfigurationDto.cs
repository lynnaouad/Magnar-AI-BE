using Magnar.AI.Application.Configuration;

namespace Magnar.AI.Application.Dto;

public sealed record ClientConfigurationDto
{
    public ReCaptchaConfiguration? ReCaptchaConfig { get; init; }

    public string? ApiUri { get; init; }

}