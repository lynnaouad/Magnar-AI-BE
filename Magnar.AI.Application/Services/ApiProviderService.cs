using Magnar.AI.Application.Dto.Providers;
using Magnar.AI.Application.Kernel;

namespace Magnar.AI.Application.Services
{
    public class ApiProviderService : IApiProviderService
    {
        #region Members
        private readonly WorkspacePluginManager workspaceManager;
        #endregion

        #region Constructor
        public ApiProviderService(WorkspacePluginManager workspaceManager)
        {
            this.workspaceManager = workspaceManager;
        }
        #endregion

        public void RegisterApis(int workspaceId, IEnumerable<ApiProviderDetails> apis, ApiProviderAuthDetailsDto authDetails)
        {
            var registry = workspaceManager.GetOrCreate(workspaceId);

            if(authDetails is null)
            {
                return;
            }

            registry.RegisterPlugins(apis, authDetails);
        }

        public void RemovePlugin(int workspaceId, string pluginName)
        {
            var registry = workspaceManager.GetOrCreate(workspaceId);
            registry.RemovePlugin(pluginName);
        }

        public KernelPluginRegistry GetKernel(int workspaceId)
        {
            return workspaceManager.GetKernel(workspaceId);
        }


        public IReadOnlyList<(string PluginName, string FunctionName, string Description)> GetFunctionsWithDetails(int workspaceId)
        {
            var registry = workspaceManager.GetOrCreate(workspaceId);
            return registry.GetAllWithDetails();
        }

    }
}
