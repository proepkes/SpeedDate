using System;
using System.Collections.Generic;
using System.IO;

namespace SpeedDate.Configuration
{
    /// <summary>
    /// Creates a new SpeedDateConfig by parsing an *.xml-file
    /// </summary>
    public class FileConfigProvider : IConfigProvider
    {
        private readonly string _configFile;

        public FileConfigProvider(string configFile)
        {
            _configFile = configFile;
        }

        public SpeedDateConfig Configure(IEnumerable<IConfig> configInstances)
        {
            var result = new SpeedDateConfig();

            var configuration = File.ReadAllText(_configFile);
            
            //xmlParser is build upon TextReader which may not support a "reset" => create a new parser for each config instance everytime we call search...
            var xmlParser = new XmlParser(configuration);
            xmlParser.Search("Network", () =>
            {
                result.Network = new NetworkConfig
                {
                    Address = xmlParser["Address"],
                    Port = Convert.ToInt32(xmlParser["Port"])
                };
            });

            xmlParser = new XmlParser(configuration);
            xmlParser.Search("Plugins", () =>
            {
                result.Plugins = new PluginsConfig
                {
                    Namespaces = xmlParser["Namespaces"]
                };
            });


            foreach (var instance in configInstances)
            {
                xmlParser = new XmlParser(configuration);
                xmlParser.Search(instance.GetType().Name, () =>
                {
                    foreach (var property in instance.GetType().GetProperties())
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

                result.Add(instance);
            }

            return result;
        }
    }
}
