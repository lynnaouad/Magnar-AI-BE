using System.Net;

namespace Magnar.AI.Application.Interfaces.Stores
{
    public interface ICookieSessionStore
    {
        CookieContainer GetOrCreateCookieContainer(int providerId);

        HttpClient CreateClientWithCookies(int providerId);

        void Refresh(int providerId);
    }
}
