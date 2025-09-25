using Magnar.AI.Application.Interfaces.Infrastructure;
using System.Collections.Concurrent;
using System.Net;

namespace Magnar.AI.Infrastructure.Stores
{
    /// <summary>
    /// A thread-safe store that manages <see cref="CookieContainer"/> instances per provider.
    /// 
    /// - Keeps a dedicated cookie jar for each provider.
    /// - Ensures cookies are isolated and reused automatically for subsequent requests.
    /// - Behaves like Postman’s cookie jar, but scoped by provider ID.
    /// - Uses <see cref="ConcurrentDictionary{TKey,TValue}"/> for safe concurrent access.
    /// 
    /// In short: provides per-provider cookie session management so you don’t have to
    /// manually attach or parse cookies on every request.
    /// </summary>
    public class CookieSessionStore : ICookieSessionStore
    {
        private readonly ConcurrentDictionary<int, CookieContainer> containers = new();

        public CookieContainer GetOrCreateCookieContainer(int providerId)
        {
            return containers.GetOrAdd(providerId, _ => new CookieContainer());
        }

        public HttpClient CreateClientWithCookies(int providerId)
        {
            return CookieSessionHttpClientFactory.CreateProviderClient(providerId, this);
        }

        public void Refresh(int providerId)
        {
            containers[providerId] = new CookieContainer();
        }
    }
}
