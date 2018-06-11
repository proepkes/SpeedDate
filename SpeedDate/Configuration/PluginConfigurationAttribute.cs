using System;

namespace SpeedDate.Configuration
{
    public sealed class PluginConfigurationAttribute : Attribute
    {
        public Type PluginType { get; }

        public PluginConfigurationAttribute(Type pluginType)
        {
            PluginType = pluginType;
        }
    }
}
