using Magnar.AI.Application.Dto.Providers;
using Magnar.AI.Application.Kernel;

namespace Magnar.AI.Application.Interfaces.Services
{
    public interface IKernelPluginService
    {
        void RegisterApiFunctions(int workspaceId, int providerId, IEnumerable<ApiProviderDetails> apis, ApiProviderAuthDetailsDto authDetails);

        void RemoveApiPlugin(int workspaceId, int providerId, string pluginName);

        void RemoveDefaultPlugin(int workspaceId, string pluginName);

        KernelPluginRegistry GetKernel(int workspaceId, int providerId);

        KernelPluginRegistry GetDefaultKernel(int workspaceId);

        void RegisterDefaultSqlFunction(int workspaceId);
    }
}
