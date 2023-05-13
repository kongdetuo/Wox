using System;

namespace Wox.Plugin
{
    public class PluginInitContext
    {
        public PluginInitContext()
        {
        }

        public PluginInitContext(PluginMetadata currentPluginMetadata, IPublicAPI aPI)
        {
            CurrentPluginMetadata = currentPluginMetadata;
            API = aPI;
        }

        public PluginMetadata CurrentPluginMetadata { get; set; }

        /// <summary>
        /// Public APIs for plugin invocation
        /// </summary>
        public IPublicAPI API { get; set; }
    }
}
