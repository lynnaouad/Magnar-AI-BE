using Magnar.AI.Application.Dto.Providers;
using Magnar.AI.Application.Helpers;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Interfaces.Managers;
using Magnar.AI.Application.Kernel;

namespace Magnar.AI.Application.Services
{
    public class KernelPluginService : IKernelPluginService
    {
        #region Members
        private readonly IKernelPluginManager workspaceManager;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IMediator mediator;
        private readonly ICookieSessionStore cookieStore;
        #endregion

        #region Constructor
        public KernelPluginService(IKernelPluginManager workspaceManager, IHttpClientFactory httpClientFactory, IMediator mediator, ICookieSessionStore cookieStore)
        {
            this.workspaceManager = workspaceManager;
            this.httpClientFactory = httpClientFactory;
            this.mediator = mediator;
            this.cookieStore = cookieStore;
        }
        #endregion

        public void RegisterApiFunctions(int workspaceId, int providerId, IEnumerable<ApiProviderDetails> apis, ApiProviderAuthDetailsDto authDetails)
        {
            if (authDetails is null)
            {
                return;
            }

            var registry = workspaceManager.GetOrCreateKernel(workspaceId, providerId);
            registry.RegisterApiPlugins(apis, authDetails, httpClientFactory, cookieStore);
        }

        public void RegisterDefaultSqlFunction(int workspaceId)
        {
            var registry = workspaceManager.GetOrCreateDefaultKernel(workspaceId);
            registry.RegisterDefaultSqlPlugin(workspaceId, mediator);
        }

        public void RemoveApiPlugin(int workspaceId, int providerId, string pluginName)
        {
            var registry = workspaceManager.GetOrCreateKernel(workspaceId, providerId);
            registry.RemovePlugin(pluginName);
        }

        public void RemoveDefaultPlugin(int workspaceId, string pluginName)
        {
            var registry = workspaceManager.GetOrCreateDefaultKernel(workspaceId);
            registry.RemovePlugin(pluginName);
        }

        public KernelPluginRegistry GetKernel(int workspaceId, int providerId)
        {
            return workspaceManager.GetOrCreateKernel(workspaceId, providerId);
        }

        public KernelPluginRegistry GetDefaultKernel(int workspaceId)
        {
            return workspaceManager.GetOrCreateDefaultKernel(workspaceId);
        }
    }
}
