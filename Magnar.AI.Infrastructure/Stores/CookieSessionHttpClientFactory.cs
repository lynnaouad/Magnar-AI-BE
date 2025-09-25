using Magnar.AI.Application.Interfaces.Infrastructure;

namespace Magnar.AI.Infrastructure.Stores
{
    public static class CookieSessionHttpClientFactory
    {
        /// <summary>
        /// Factory for creating <see cref="HttpClient"/> instances that are bound to
        /// a provider-specific <see cref="CookieContainer"/>.
        ///
        /// - Ensures each provider has its own isolated cookie session.
        /// - Automatically reuses cookies across multiple requests for the same provider.
        /// - Useful for cookie-based authentication flows (e.g., SAP B1 Service Layer).
        /// - Configures the handler to bypass certificate validation for development/self-signed certs.
        /// 
        /// In short: creates HttpClients that behave like Postman’s per-host cookie jar,
        /// but scoped by provider ID.
        /// </summary>
        public static HttpClient CreateProviderClient(int providerId, ICookieSessionStore store)

        {
            var container = store.GetOrCreateCookieContainer(providerId);

            var handler = new HttpClientHandler
            {
                CookieContainer = container,
                UseCookies = true,
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            };

            return new HttpClient(handler, disposeHandler: true);
        }
    }
}
