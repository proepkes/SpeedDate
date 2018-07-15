using System;
using System.Collections.Generic;
using System.Text;

namespace SpeedDate.Configuration
{
    public class DefaultConfigProvider : IConfigProvider
    {
        public SpeedDateConfig Result { get; }
        
        public DefaultConfigProvider(NetworkConfig networkConfig, 
            PluginsConfig pluginsConfig, 
            IEnumerable<IConfig> additionalConfigs = null)
        {
            Result = new SpeedDateConfig
            {
                Network = networkConfig,
                Plugins = pluginsConfig
            };

            if (additionalConfigs == null) 
                return;
            
            foreach (var config in additionalConfigs)
            {
                Result.Add(config);
            }
        }
        
        public void Configure(IEnumerable<IConfig> configInstances)
        {
        }
    }
}
