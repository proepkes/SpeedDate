using System;
using System.IO;

namespace SpeedDate.Configuration.SmartConf.Sources
{
    /// <summary>
    /// An IConfigurationSource fed from an XML file,
    /// serialized by the default XmlSerializer.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class XmlFileConfigurationSource<T> : XmlStreamConfigurationSource<T> where T : class
    {
        private string Filename { get; set; }

        /// <summary>
        /// This configuration source can be saved.
        /// </summary>
        public override bool ReadOnly { get { return false; } }

        /// <summary>
        /// Load the configuration object from a file.
        /// Filename is generated from some other source,
        /// such as another IConfigurationSource.
        /// </summary>
        /// <param name="getFilename">Function to generate filename.</param>
        public XmlFileConfigurationSource(Func<string> getFilename)
            : this(getFilename())
        {
        }

        /// <summary>
        /// Load the configuration object from a file.
        /// </summary>
        /// <param name="filename">Filename to load.</param>
        public XmlFileConfigurationSource(string filename)
            : base(null)
        {
            Filename = filename;
        }

        protected override Stream GetInputStream()
        {
            return new FileStream(
                Filename, FileMode.Open, FileAccess.Read, FileShare.Read);
        }

        protected override Stream GetOutputStream()
        {
            return new FileStream(Filename, FileMode.Create);
        }
    }
}
