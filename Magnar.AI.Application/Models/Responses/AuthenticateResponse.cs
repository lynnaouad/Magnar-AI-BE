using System.Text.Json.Serialization;

namespace Magnar.AI.Application.Models.Responses;

public sealed class AuthenticateResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; } = string.Empty;

    [JsonPropertyName("error")]
    public string Error { get; set; } = string.Empty;

    public string UsernameOrEmail { get; set; } = string.Empty;

    public int UserId { get; set; }
}
