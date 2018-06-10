using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using SpeedDate.Configuration;

//using Microsoft.Extensions.Configuration;

namespace SpeedDate
{
    public sealed class SpeedDateConfig
    {
        private static string _configFile;

        public static NetworkConfig Network = new NetworkConfig();

        public static PluginsConfig Plugins = new PluginsConfig();

        public static void FromXml(string configFile)
        {
            _configFile = configFile;

            var xmlParser = new XmlParser(new StreamReader(configFile));
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
            var attribute = typeof(T).GetCustomAttribute(typeof(PluginConfigurationAttribute)) as PluginConfigurationAttribute;
            if (attribute == null)
            {
                throw new InvalidDataContractException("Configuration-classes require the [PluginConfiguration]-Attribute");
            }

            var instance = new T();
            var xmlParser = new XmlParser(new StreamReader(_configFile));
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

        //private static T Get<T>(string tag) where T: class, new()
        //{
        //    _xmlParser.Search(tag, () =>
        //    {
        //        var instance = new T();
        //        foreach (var property in typeof(T).GetProperties())
        //        {
        //            var configValue = _xmlParser[property.Name];
        //            if (configValue != null)
        //            {
        //                property.SetValue(instance, configValue);
        //            }
        //        }
        //    });
        //    return default(T);
        //}
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

        public static NetworkConfig FromXmlElement(XElement element)
        {
            //element.desc

            var networkConfig = new NetworkConfig();
            //networkConfig.Address 
            return networkConfig;
        }
    }
    
    internal class XmlParser : IDisposable
    {
        public TextReader Stream { get; private set; }
        public string Text { get; private set; }
        public string Tag { get; private set; }

        public XmlParser(TextReader stream) { Stream = stream; }
        public XmlParser(string source) : this(new StringReader(source)) { }

        public void Dispose()
        {
            Stream?.Dispose();
            Stream = null;
        }

        private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

        public string this[string key] => _values.TryGetValue(key, out var ret) ? ret : null;

        private string _reserved;

        public bool Read()
        {
            Text = Tag = null;
            _values.Clear();
            if (Stream == null) return false;

            if (_reserved != null)
            {
                Tag = _reserved;
                _reserved = null;
            }
            else
                ReadText();
            return true;
        }
        public void Search(string tag, Action a)
        {
            while (Read())
            {
                if (Tag != tag)
                    continue;

                a();
                break;
            }
        }

        public void Search(string tag, Func<bool> f, Action a)
        {
            while (Read())
            {
                if (Tag != tag || !f())
                    continue;

                a();
                break;
            }
        }
        public void SearchEach(string tag, Action a)
        {
            var end = "/" + Tag;
            while (Read())
            {
                if (Tag == end)
                    break;
                if (Tag == tag)
                    a();
            }
        }

        public void SearchEach(string tag, Func<bool> f, Action a)
        {
            var end = "/" + Tag;
            while (Read())
            {
                if (Tag == end)
                    break;
                if (Tag == tag && f())
                    a();
            }
        }

        private int _current;

        private int ReadChar()
        {
            if (Stream == null) return _current = -1;
            _current = Stream.Read();
            if (_current == -1) Dispose();
            return _current;
        }

        private void ReadText()
        {
            var text = new StringBuilder();
            int ch;
            while ((ch = ReadChar()) != -1)
            {
                if (ch == '<') break;
                text.Append((char)ch);
            }
            Text = FromEntity(text.ToString());
            if (ch == '<') ReadTag();
        }

        private void ReadTag()
        {
            int ch;
            var tag = new StringBuilder();
            while ((ch = ReadChar()) != -1)
            {
                if (ch == '>' || (ch == '/' && tag.Length > 0))
                    break;
                else if (ch > ' ')
                {
                    tag.Append((char)ch);
                    if (tag.Length == 3 && tag.ToString() == "!--") break;
                }
                else if (tag.Length > 0)
                    break;
            }
            Tag = tag.ToString();
            if (ch == '/')
            {
                _reserved = "/" + Tag;
                ch = ReadChar();
            }
            if (ch != '>')
            {
                if (Tag == "!--")
                    ReadComment();
                else
                    while (ReadAttribute()) ;
            }
        }

        private void ReadComment()
        {
            int ch, m = 0;
            var comment = new StringBuilder();
            while ((ch = ReadChar()) != -1)
            {
                if (ch == '>' && m >= 2)
                {
                    comment.Length -= 2;
                    break;
                }
                comment.Append((char)ch);
                if (ch == '-') m++; else m = 0;
            }
            _values["comment"] = comment.ToString();
        }

        private bool ReadAttribute()
        {
            string name = null;
            do
            {
                var n = ReadValue();
                switch (_current)
                {
                    case '>':
                        return false;
                    case '/':
                        _reserved = "/" + Tag;
                        break;
                }
                if (n != "") name = n;
            } while (_current != '=');
            var value = ReadValue();
            if (name != null) _values[name] = value;
            return _current != '>';
        }

        private string ReadValue()
        {
            int ch;
            var value = new StringBuilder();
            while ((ch = ReadChar()) != -1)
            {
                if (ch == '>' || ch == '=' || ch == '/')
                    break;
                else if (ch == '"')
                {
                    while ((ch = ReadChar()) != -1)
                    {
                        if (ch == '"') break;
                        value.Append((char)ch);
                    }
                    break;
                }
                else if (ch > ' ')
                    value.Append((char)ch);
                else if (value.Length > 0)
                    break;
            }
            return value.ToString();
        }

        public static string FromEntity(string s)
        {
            return s
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Replace("&quot;", "\"")
                .Replace("&nbsp;", " ")
                .Replace("&amp;", "&");
        }
    }
}
