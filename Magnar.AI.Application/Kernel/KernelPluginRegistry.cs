using Magnar.AI.Application.Dto.Providers;
using Magnar.AI.Application.Helpers;
using Magnar.AI.Application.Interfaces.Infrastructure;
using Microsoft.SemanticKernel;

namespace Magnar.AI.Application.Kernel
{
    /// <summary>
    /// Manages the registration and lifecycle of Semantic Kernel plugins within a given <see cref="Kernel"/>.
    /// Supports registering API-based plugins and a fallback SQL assistant plugin.
    /// </summary>
    public class KernelPluginRegistry 
    {
        #region Members
        /// <summary>
        /// The Semantic Kernel instance where plugins are registered.
        /// </summary>
        public Microsoft.SemanticKernel.Kernel Kernel { get; }
        #endregion

        #region Constructor
        public KernelPluginRegistry(Microsoft.SemanticKernel.Kernel Kernel)
        {
            this.Kernel = Kernel;
        }
        #endregion

        /// <summary>
        /// Registers or replaces plugins in the kernel, each built from a group of API definitions.
        /// </summary>
        /// <param name="apis">
        /// A collection of <see cref="ApiProviderDetails"/>.  
        /// Items are grouped by <c>PluginName</c>, and each group is registered as a separate plugin.
        /// </param>
        /// <param name="authDetails">Authentication details used when invoking API functions.</param>
        /// <param name="httpClientFactory">Factory used to create HTTP clients for API calls.</param>
        /// <remarks>
        /// - If a plugin with the same name already exists, it is removed and replaced.  
        /// - Each <see cref="ApiProviderDetails"/> in a group is converted to a <see cref="KernelFunction"/> 
        ///   and included in the corresponding plugin.  
        /// - This method allows registering multiple plugins in a single call.
        /// </remarks>
        public void RegisterApiPlugins(IEnumerable<ApiProviderDetails> apis, ApiProviderAuthDetailsDto authDetails, IHttpClientFactory httpClientFactory, ICookieSessionStore cookieStore)
        {
            if(apis is null || !apis.Any() || authDetails is null)
            {
                return;
            }

            var grouped = apis.GroupBy(api => api.PluginName).ToList();

            foreach (var group in grouped)
            {
                var pluginName = group.Key;
                var pluginApis = group;

                RegisterPlugin(pluginName, pluginApis, authDetails, httpClientFactory, cookieStore);
            }
        }

        /// <summary>
        /// Removes a plugin from the kernel by its name, if it exists.
        /// </summary>
        /// <param name="pluginName">The name of the plugin to remove.</param>
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

        /// <summary>
        /// Registers the default SQL plugin for the specified workspace.
        /// This plugin generates and executes SQL queries when no other plugin matches.
        /// </summary>
        /// <param name="workspaceId">The workspace ID used to scope SQL generation.</param>
        /// <param name="mediator">Mediator used to handle SQL generation commands.</param>
        public void RegisterDefaultSqlPlugin(int workspaceId, IMediator mediator)
        {
            try
            {
                var pluginName = Constants.KernelPluginsNames.DefaultPlugin;

                RemovePlugin(pluginName);

                var function = KernelPluginFactory.CreateFallbackSqlFunction(workspaceId, mediator);

                Kernel.Plugins.AddFromFunctions(pluginName, [function]);
            }
            finally { };
        }

        #region Private Methods

        private void RegisterPlugin(string pluginName, IEnumerable<ApiProviderDetails> apis, ApiProviderAuthDetailsDto authDetails, IHttpClientFactory httpClientFactory, ICookieSessionStore cookieStore)
        {
            try
            {
                RemovePlugin(pluginName);

                List<KernelFunction> functions = [];

                foreach (var api in apis)
                {
                    functions.Add(KernelPluginFactory.CreateApiFunction(api, authDetails, httpClientFactory, cookieStore));
                }

                Kernel.Plugins.AddFromFunctions(pluginName, functions);
            }
            finally{ };
        }

        #endregion
    }
}
