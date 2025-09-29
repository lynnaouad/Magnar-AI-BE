using Magnar.AI.Application.Dto.Providers;
using Magnar.AI.Application.Kernel;

namespace Magnar.AI.Application.Interfaces.Services
{
    public interface IKernelPluginService
    {
        void RegisterApiFunctions(int workspaceId, int providerId, IEnumerable<ApiProviderDetailsDto> apis, ApiProviderAuthDetailsDto authDetails);

        void RemoveApiPlugin(int workspaceId, int providerId, string pluginName);

        KernelPluginRegistry GetKernel(int workspaceId, int providerId);
    }
}
