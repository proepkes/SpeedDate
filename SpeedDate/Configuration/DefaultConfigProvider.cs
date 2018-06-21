using System;
using System.Collections.Generic;
using System.Text;

namespace SpeedDate.Configuration
{
    public class DefaultConfigProvider : IConfigProvider
    {
        private readonly SpeedDateConfig _result;
        
        public DefaultConfigProvider(NetworkConfig networkConfig, 
            PluginsConfig pluginsConfig, 
            IEnumerable<IConfig> additionalConfigs = null)
        {
            _result = new SpeedDateConfig
            {
                Network = networkConfig,
                Plugins = pluginsConfig
            };

            if (additionalConfigs == null) 
                return;
            
            foreach (var config in additionalConfigs)
            {
                _result.Add(config);
            }
        }
        
        public SpeedDateConfig Configure(IEnumerable<IConfig> configInstances)
        {
            return _result;
        }
    }
}
