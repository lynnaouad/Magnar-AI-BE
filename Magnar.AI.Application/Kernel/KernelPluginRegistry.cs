using Magnar.AI.Application.Dto.Providers;
using Microsoft.SemanticKernel;

namespace Magnar.AI.Application.Kernel
{
    public class KernelPluginRegistry
    {
        #region Members
        public Microsoft.SemanticKernel.Kernel Kernel { get; }
        public IHttpClientFactory httpClientFactory { get; }
        #endregion

        #region Constructor
        public KernelPluginRegistry(Microsoft.SemanticKernel.Kernel kernel, IHttpClientFactory httpClientFactory)
        {
            Kernel = kernel;
            this.httpClientFactory = httpClientFactory;
        }
        #endregion

        /// <summary>
        /// Registers or replaces multiple plugins in the kernel, each built from a group of API definitions.
        /// </summary>
        /// <param name="apis">
        /// A collection of <see cref="ApiProviderDetails"/>.  
        /// Items are grouped by <c>PluginName</c>, and each group is registered as a separate plugin.
        /// </param>
        /// <remarks>
        /// - If a plugin with the same name already exists, it is removed and replaced.  
        /// - Each <see cref="ApiProviderDetails"/> in a group is converted to a <see cref="KernelFunction"/> 
        /// and included in the corresponding plugin.  
        /// - This method allows registering multiple plugins in a single call.
        /// </remarks>
        public void RegisterPlugins(IEnumerable<ApiProviderDetails> apis, ApiProviderAuthDetailsDto authDetails)
        {
            if(apis is null || !apis.Any() || authDetails is null)
            {
                return;
            }

            var grouped = apis.GroupBy(api => api.PluginName).ToList();

            foreach (var group in grouped)
            {
                var pluginName = group.Key;
                var pluginApis = group.ToList();

                RegisterPlugin(pluginName, pluginApis, authDetails);
            }
        }

        public void RemovePlugin(string pluginName)
        {
            if (string.IsNullOrEmpty(pluginName))
            {
                return;
            }

            if (Kernel.Plugins.TryGetPlugin(pluginName, out var plugin))
            {
                Kernel.Plugins.Remove(plugin);
            }
        }

        public IReadOnlyList<(string PluginName, string FunctionName, string Description)> GetAllWithDetails()
        {
            var result = new List<(string PluginName, string FunctionName, string Description)>();

            foreach (var plugin in Kernel.Plugins)
            {
                foreach (var fn in plugin.GetFunctionsMetadata())
                {
                    result.Add((
                        PluginName: plugin.Name,
                        FunctionName: fn.Name,
                        Description: fn.Description ?? string.Empty
                    ));
                }
            }

            return result;
        }
  
        #region Private Methods

        private void RegisterPlugin(string pluginName, IEnumerable<ApiProviderDetails> apis, ApiProviderAuthDetailsDto authDetails)
        {
            try
            {
                RemovePlugin(pluginName);

                var functions = CreateFunctions(apis, authDetails);

                Kernel.Plugins.AddFromFunctions(pluginName, functions);
            }
            finally{ };
        }

        private IEnumerable<KernelFunction> CreateFunctions(IEnumerable<ApiProviderDetails> apis, ApiProviderAuthDetailsDto authDetails)
        {
            List<KernelFunction> functions = [];

            foreach (var api in apis)
            {
                functions.Add(KernelPluginFactory.CreateFromMethod(api, authDetails, httpClientFactory));
            }

            return functions;
        }

        #endregion
    }
}
