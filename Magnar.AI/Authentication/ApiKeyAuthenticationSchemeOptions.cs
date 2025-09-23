using Microsoft.AspNetCore.Authentication;

namespace Magnar.AI.Authentication
{
    public class ApiKeyAuthenticationSchemeOptions : AuthenticationSchemeOptions
    {
        public const string DefaultScheme = "ApiKey";

        public string Scheme => DefaultScheme;

        public string AuthenticationType = DefaultScheme;
    }
}