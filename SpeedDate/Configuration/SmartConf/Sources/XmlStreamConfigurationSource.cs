using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace SpeedDate.Configuration.SmartConf.Sources
{
    /// <summary>
    /// Load the configuration object from an XML stream.
    /// This configuration source is read-only and cannot
    /// be saved.
    /// </summary>
    /// <typeparam name="T">Configuration object type to deserialize.</typeparam>
    public class XmlStreamConfigurationSource<T> : IConfigurationSource<T> where T : class
    {
        private readonly XmlSerializer _serializer;

        public bool PrimarySource { get; set; }

        public bool Required { get; set; }

        public virtual bool ReadOnly { get { return true; } }

        private readonly Stream _sourceStream;

        /// <summary>
        /// Load an XML configuration from the given stream.
        /// </summary>
        /// <param name="stream">Stream to serialize.</param>
        public XmlStreamConfigurationSource(Stream stream)
        {
            _sourceStream = stream;
            _serializer = new XmlSerializer(typeof(T));
        }
        

        private T _config;

        public T Config
        {
            get
            {
                if (_config == null)
                {
                    using (var stream = GetInputStream())
                    {
                        _config = (T)_serializer.Deserialize(stream);
                    }
                }
                return _config;
            }
        }

        protected virtual Stream GetInputStream()
        {
            if (_sourceStream == null)
            {
                throw new ArgumentException("Input stream cannot be null.");
            }
            return _sourceStream;
        }

        protected virtual Stream GetOutputStream()
        {
            throw new InvalidOperationException(
                "Cannot write to an XmlStreamConfigurationSource.");
        }

        public void Invalidate()
        {
            _config = null;
        }

        public void Save(T obj)
        {
            PartialSave(obj, null);
        }

        public void PartialSave(T obj, IEnumerable<string> propertyNames)
        {
            var attributeOverrides = new XmlAttributeOverrides();

            if (propertyNames != null)
            {
                var properties = new HashSet<string>(
                    typeof (T).GetProperties().Select(p => p.Name))
                    .Except(propertyNames);
                foreach (var prop in properties)
                {
                    attributeOverrides.Add(
                        typeof(T), prop, new XmlAttributes { XmlIgnore = true });
                }
            }

            var serializer = new XmlSerializer(typeof(T), attributeOverrides);
            using (var writer = GetOutputStream())
            {
                serializer.Serialize(writer, obj);
            }           
        }
    }
}
