using Magnar.AI.Application.Dto.Providers;

namespace Magnar.AI.Application.Kernel
{
    public class WorkspacePluginManager
    {
        #region Members
        private readonly Dictionary<int, KernelPluginRegistry> registries = [];
        private readonly IHttpClientFactory httpClientFactory;
        #endregion

        #region Constructor
        public WorkspacePluginManager(IHttpClientFactory httpClientFactory)
        {
            this.httpClientFactory = httpClientFactory;
        }
        #endregion

        public KernelPluginRegistry GetOrCreate(int workspaceId)
        {
            if (!registries.TryGetValue(workspaceId, out var registry))
            {
                registry = CreateRegistry();
                registries[workspaceId] = registry;
            }

            return registry;
        }

        /// <summary>
        /// Completely rebuilds the kernel for a workspace from the DB.
        /// </summary>
        public KernelPluginRegistry RebuildKernel(int workspaceId, IEnumerable<ApiProviderDetails> apis, ApiProviderAuthDetailsDto authDetails)
        {
            var registry = CreateRegistry();

            if (authDetails is null)
            {
                return registry;
            }

            registry.RegisterPlugins(apis, authDetails);

            registries[workspaceId] = registry;

            return registry;
        }

        public IReadOnlyList<(string PluginName, string FunctionName, string Description)> GetFunctionsWithDetails(int workspaceId)
        {
            var registry = GetOrCreate(workspaceId);
            return registry.GetAllWithDetails();
        }

        public KernelPluginRegistry GetKernel(int workspaceId)
        {
            var registry = GetOrCreate(workspaceId);
            return registry;
        }

        #region Private Methods

        private KernelPluginRegistry CreateRegistry()
        {
            var kernel = new Microsoft.SemanticKernel.Kernel();
            return new KernelPluginRegistry(kernel, httpClientFactory);
        }

        #endregion
    }
}
