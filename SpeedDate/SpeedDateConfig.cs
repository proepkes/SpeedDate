using System;
using System.IO;
using System.Net;
using Microsoft.Extensions.Configuration;

namespace SpeedDate
{
    public sealed class SpeedDateConfig
    {
        private static IConfigurationRoot _config;

        public static NetworkConfig Network;

        public static PluginsConfig Plugins;

        public static void Initialize(string configFile)
        {
            _config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile(configFile, false, true)
                .AddEnvironmentVariables().Build();


            Plugins = Get<PluginsConfig>(nameof(Plugins));
            Network = Get<NetworkConfig>(nameof(Network));
        }

        public static T GetPluginConfig<T>()
        {
            return Get<T>("Plugins:" + typeof(T).Name.Replace("Config", string.Empty));
        }

        private static T Get<T>(string configPath)
        {
            var section = _config.GetSection(configPath);
            if (section != null)
            {
                return section.Get<T>();
            }
            return default(T);
        }
    }

    public class PluginsConfig
    {
        public string SearchPath { get; set; }
        public bool CreateDirIfNotExists { get; set; }
        public bool LoadAll { get; set; }
        public string PluginsNamespace { get; set; }
    }

    public class NetworkConfig
    {
        public string IP { get; set; }
        public int Port { get; set; }
    }
}
