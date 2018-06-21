using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
        public static readonly PluginsConfig LoadAllPlugins = new PluginsConfig();
        public string Namespaces { get; set; }

        public PluginsConfig(string namespaces = "*")
        {
            Namespaces = namespaces;
        }
    }

    public class NetworkConfig
    {
        public string Address { get; set; }
        public int Port { get; set; }

        public override string ToString()
        {
            return $"{Address}:{Port}";
        }

        public NetworkConfig(string address = default(string) , int port = default(int))
        {
            Address = address;
            Port = port;
        }
        public NetworkConfig(IPAddress address, int port = default(int))
        {
            Address = address.ToString();
            Port = port;
        }
    }
}
