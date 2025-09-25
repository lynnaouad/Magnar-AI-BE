using System.Net;

namespace Magnar.AI.Application.Interfaces.Infrastructure
{
    public interface ICookieSessionStore
    {
        CookieContainer GetOrCreateCookieContainer(int providerId);

        HttpClient CreateClientWithCookies(int providerId);

        void Refresh(int providerId);
    }
}
