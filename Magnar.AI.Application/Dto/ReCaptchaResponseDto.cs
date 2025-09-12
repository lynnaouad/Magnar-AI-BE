using System.Text.Json.Serialization;

namespace Magnar.AI.Application.Dto;

public sealed record ReCaptchaResponseDto
{
    [JsonPropertyName("success")]
    public bool Success { get; init; }

    [JsonPropertyName("challenge_ts")]
    public string ValidatedDateTime { get; init; } = string.Empty;

    [JsonPropertyName("hostname")]
    public string HostName { get; init; } = string.Empty;

    [JsonPropertyName("error-codes")]
    public List<string> ErrorCodes { get; init; } = [];
}
