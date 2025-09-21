namespace Magnar.AI.Application.Dto.Providers
{
    public class ApiProviderAuthDetailsDto
    {
        public AuthType AuthType { get; set; }

        // Common
        public string? TokenUrl { get; set; }

        // OAuth2
        public string? ClientId { get; set; }
        public string? ClientSecret { get; set; }
        public string? Scope { get; set; }

        // Password Grant
        public string? Username { get; set; }
        public string? Password { get; set; }

        // API Key
        public string? ApiKeyName { get; set; }
        public string? ApiKeyValue { get; set; }
    }
}
