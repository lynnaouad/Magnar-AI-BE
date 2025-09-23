using Magnar.AI.Application.Dto.Providers;
using Magnar.AI.Application.Kernel;

namespace Magnar.AI.Application.Interfaces.Managers
{
    public interface IKernelPluginManager
    {
        /// <summary>
        /// Creates plugin registry (kernel) per workspaceId and providerId.
        /// </summary>
        KernelPluginRegistry GetOrCreateKernel(int workspaceId, int providerId);

        /// <summary>
        /// Creates default plugin registry (kernel) per workspaceId for sql query generation.
        /// </summary>
        KernelPluginRegistry GetOrCreateDefaultKernel(int workspaceId);

        /// <summary>
        /// Completely rebuilds the kernel for a <paramref name="providerId"/> in a <paramref name="workspaceId"/> from the apis registered in DB.
        /// </summary>
        KernelPluginRegistry RebuildKernel(int workspaceId, int providerId, IEnumerable<ApiProviderDetails> apis, ApiProviderAuthDetailsDto authDetails);
    }
}
