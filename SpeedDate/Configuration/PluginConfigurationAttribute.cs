using System;
using System.Collections.Generic;
using System.Text;

namespace SpeedDate.Configuration
{
    public class PluginConfigurationAttribute : Attribute
    {
        public PluginConfigurationAttribute(Type pluginType)
        {
            PluginType = pluginType;
        }

        public Type PluginType { get; }
    }
}
