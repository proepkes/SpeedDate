using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using SpeedDate.Configuration;

namespace SpeedDate
{
    public sealed class SpeedDateConfig
    {
        private static string _configuration;

        public static readonly NetworkConfig Network = new NetworkConfig();

        public static readonly PluginsConfig Plugins = new PluginsConfig();

        public static void FromXml(string configFile)
        {
            _configuration = File.ReadAllText(configFile);

            var xmlParser = new XmlParser(_configuration);
            xmlParser.Search("Network", () =>
            {
                Network.Address = xmlParser["Address"];
                Network.Port = Convert.ToInt32(xmlParser["Port"]);
            });

            xmlParser.Search("Plugins",  () =>
            {
                Plugins.LoadAll = Convert.ToBoolean(xmlParser["LoadAll"]);
                Plugins.PluginsNamespaces = xmlParser["PluginsNamespaces"];
            });

       
        }

        public static T Get<T>() where T : class, new()
        {
            if (!(typeof(T).GetCustomAttribute(typeof(PluginConfigurationAttribute)) is PluginConfigurationAttribute attribute))
            {
                throw new InvalidDataContractException("Configuration-classes require the [PluginConfiguration]-Attribute");
            }

            var instance = new T();
            var xmlParser = new XmlParser(_configuration);
            xmlParser.Search(attribute.PluginType.Name, () =>
            {
                foreach (var property in typeof(T).GetProperties())
                {
                    var configValue = xmlParser[property.Name];
                    if (configValue != null)
                    {
                        switch (Type.GetTypeCode(property.PropertyType))
                        {
                            case TypeCode.Boolean:
                                property.SetValue(instance, bool.Parse(configValue));
                                break;
                            case TypeCode.Int32:
                                property.SetValue(instance, int.Parse(configValue));
                                break;
                            default:
                                property.SetValue(instance, configValue);
                                break;
                        }
                    }
                }
            });
            return instance;
        }
    }

    public class PluginsConfig
    {
        public bool LoadAll { get; set; }
        public string PluginsNamespaces { get; set; }
    }

    public class NetworkConfig
    {
        public string Address { get; set; }
        public int Port { get; set; }
    }
}
