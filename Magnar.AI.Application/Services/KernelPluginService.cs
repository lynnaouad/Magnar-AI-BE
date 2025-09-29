using Magnar.AI.Application.Dto.Providers;
using Magnar.AI.Application.Interfaces.Managers;
using Magnar.AI.Application.Interfaces.Stores;
using Magnar.AI.Application.Kernel;
using Magnar.AI.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;

namespace Magnar.AI.Application.Services
{
    public class KernelPluginService : IKernelPluginService
    {
        #region Members
        private readonly IKernelPluginManager workspaceManager;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IServiceScopeFactory scopeFactory;
        private readonly ICookieSessionStore cookieStore;
        #endregion

        #region Constructor
        public KernelPluginService(IKernelPluginManager workspaceManager, IHttpClientFactory httpClientFactory, IServiceScopeFactory scopeFactory, ICookieSessionStore cookieStore)
        {
            this.workspaceManager = workspaceManager;
            this.httpClientFactory = httpClientFactory;
            this.scopeFactory = scopeFactory;
            this.cookieStore = cookieStore;
        }
        #endregion

        public void RegisterApiFunctions(int workspaceId, int providerId, IEnumerable<ApiProviderDetailsDto> apis, ApiProviderAuthDetailsDto authDetails)
        {
            if (authDetails is null)
            {
                return;
            }

            var registry = workspaceManager.GetOrCreateKernel(workspaceId, providerId);
            registry.RegisterApiPlugins(workspaceId, apis, authDetails, httpClientFactory, cookieStore, scopeFactory);
        }

        public void RemoveApiPlugin(int workspaceId, int providerId, string pluginName)
        {
            var registry = workspaceManager.GetOrCreateKernel(workspaceId, providerId);
            registry.RemovePlugin(pluginName);
        }

        public KernelPluginRegistry GetKernel(int workspaceId, int providerId)
        {
            return workspaceManager.GetOrCreateKernel(workspaceId, providerId);
        }
    }
}
