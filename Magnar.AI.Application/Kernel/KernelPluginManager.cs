using Magnar.AI.Application.Dto.Providers;
using Magnar.AI.Application.Helpers;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Magnar.AI.Application.Interfaces.Managers;

namespace Magnar.AI.Application.Kernel
{
    /// <summary>
    /// Manages <see cref="KernelPluginRegistry"/> instances across workspaces and providers.
    /// Provides methods to create, rebuild, and retrieve kernel registries that store registered plugins.
    /// </summary>
    public class KernelPluginManager : IKernelPluginManager
    {
        #region Members
        /// <summary>
        /// Stores kernel plugin registries keyed by <c>(workspaceId, providerId)</c>.
        /// </summary>
        private readonly Dictionary<(int WorkspaceId, int ProviderId), KernelPluginRegistry> registries = [];

        /// <summary>
        /// Stores default kernel plugin registries keyed by <c>workspaceId</c>.
        /// </summary>
        private readonly Dictionary<int, KernelPluginRegistry> defaultRegistries = [];

        private readonly IHttpClientFactory httpClientFactory;

        private readonly ICookieSessionStore cookieStore;
        #endregion

        #region Constructor
        public KernelPluginManager(IHttpClientFactory httpClientFactory, ICookieSessionStore cookieStore)
        {
            this.httpClientFactory = httpClientFactory;
            this.cookieStore = cookieStore;
        }
        #endregion

        /// <summary>
        /// Retrieves an existing <see cref="KernelPluginRegistry"/> for the specified workspace and provider,
        /// or creates a new one if it does not exist.
        /// </summary>
        /// <param name="workspaceId">The workspace identifier.</param>
        /// <param name="providerId">The provider identifier.</param>
        /// <returns>A <see cref="KernelPluginRegistry"/> instance associated with the given keys.</returns>
        public KernelPluginRegistry GetOrCreateKernel(int workspaceId, int providerId)
        {
            var key = (workspaceId, providerId);
            if (!registries.TryGetValue(key, out var registry))
            {
                registry = CreateKernelPluginRegistry();
                registries[key] = registry;
            }

            return registry;
        }

        /// <summary>
        /// Rebuilds a kernel for the given workspace and provider using a new set of API plugin definitions.
        /// Existing registry is replaced with a fresh instance containing the specified plugins.
        /// </summary>
        /// <param name="workspaceId">The workspace identifier.</param>
        /// <param name="providerId">The provider identifier.</param>
        /// <param name="apis">A collection of API provider details to register as plugins.</param>
        /// <param name="authDetails">Authentication details for invoking API plugins.</param>
        /// <returns>A new <see cref="KernelPluginRegistry"/> containing the rebuilt plugins.</returns>
        public KernelPluginRegistry RebuildKernel(int workspaceId, int providerId, IEnumerable<ApiProviderDetails> apis, ApiProviderAuthDetailsDto authDetails)
        {
            var registry = CreateKernelPluginRegistry();

            if (authDetails is null)
            {
                return registry;
            }

            registry.RegisterApiPlugins(apis, authDetails, httpClientFactory, cookieStore);

            var key = (workspaceId, providerId);

            registries[key] = registry;

            return registry;
        }

        /// <summary>
        /// Retrieves the default <see cref="KernelPluginRegistry"/> for a given workspace,
        /// or creates a new one if it does not exist.
        /// </summary>
        /// <param name="workspaceId">The workspace identifier.</param>
        /// <returns>A <see cref="KernelPluginRegistry"/> representing the default kernel for the workspace.</returns>
        public KernelPluginRegistry GetOrCreateDefaultKernel(int workspaceId)
        {
            if (!defaultRegistries.TryGetValue(workspaceId, out var registry))
            {
                registry = CreateKernelPluginRegistry();
                defaultRegistries[workspaceId] = registry;
            }

            return registry;
        }

        #region Private Methods

        /// <summary>
        /// Creates a new <see cref="KernelPluginRegistry"/> with a fresh Semantic Kernel instance.
        /// </summary>
        /// <returns>A new <see cref="KernelPluginRegistry"/>.</returns>
        private KernelPluginRegistry CreateKernelPluginRegistry()
        {
            return new KernelPluginRegistry(new Microsoft.SemanticKernel.Kernel());
        }

        #endregion
    }
}
