using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;

namespace SpeedDate.Configuration
{
    public sealed class SpeedDateConfig
    {
        private readonly List<IConfig> _pluginConfigs = new List<IConfig>();

        public NetworkConfig Network { get; internal set; }

        public PluginsConfig Plugins { get; internal set; }

        public void Add(IConfig config)
        {
            _pluginConfigs.Add(config);
        }

        public bool TryGetConfig(string typeName, out IConfig result)
        {
            result = _pluginConfigs.FirstOrDefault(config => config.GetType().FullName.Equals(typeName));
            return result != null;
        }
    }

    public class PluginsConfig
    {
        public bool LoadAll { get; set; }
        public string PluginsNamespaces { get; set; }

        public PluginsConfig(bool loadAll = default(bool), string pluginsNamespaces = default(string))
        {
            LoadAll = loadAll;
            PluginsNamespaces = pluginsNamespaces;
        }
    }

    public class NetworkConfig
    {
        public string Address { get; set; }
        public int Port { get; set; }

        public NetworkConfig(string address = default(string) , int port = default(int))
        {
            Address = address;
            Port = port;
        }
    }
}
