using Magnar.AI.Application.Dto.Providers;
using Magnar.AI.Application.Kernel;

namespace Magnar.AI.Application.Interfaces.Services
{
    public interface IApiProviderService
    {
        void RegisterApis(int workspaceId, IEnumerable<ApiProviderDetails> apis, ApiProviderAuthDetailsDto authDetails);

        void RemovePlugin(int workspaceId, string pluginName);

        KernelPluginRegistry GetKernel(int workspaceId);

        IReadOnlyList<(string PluginName, string FunctionName, string Description)> GetFunctionsWithDetails(int workspaceId);
    }
}
