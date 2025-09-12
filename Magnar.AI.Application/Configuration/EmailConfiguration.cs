using Microsoft.Extensions.Configuration;

namespace Magnar.AI.Application.Configuration;

public sealed record EmailConfiguration
{
    public const string SectionName = Constants.Configuration.Sections.Email;

    public string? Host { get; init; }

    public int Port { get; init; }

    public string? MailUid { get; init; }

    public string? Password { get; init; }

    public bool SSLEnabled { get; init; }

    public string? FromDisplayName { get; init; }

    [ConfigurationKeyName("TimeoutInMilliseconds")]
    public int Timeout { get; init; } = 100000;

}