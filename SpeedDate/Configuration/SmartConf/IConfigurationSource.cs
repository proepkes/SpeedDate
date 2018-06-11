using System;
using System.Collections.Generic;

namespace SpeedDate.Configuration.SmartConf
{
    /// <summary>
    /// A source for creating configuration objects of
    /// type <typeparam name="T"></typeparam>.
    /// </summary>
    public interface IConfigurationSource<T> where T : class
    {
        /// <summary>
        /// Indicate that this source should be the default
        /// source to save changes to.
        /// </summary>
        bool PrimarySource { get; set; }

        /// <summary>
        /// Indicate that this source is required to run.
        /// If not required and loading the config object fails,
        /// the source will be skipped.
        /// </summary>
        bool Required { get; set; }

        /// <summary>
        /// Indicate that a source is read only. Attempts to
        /// <see cref="Save"/> or <see cref="PartialSave"/>
        /// this source should throw an
        /// <see cref="InvalidOperationException"/>.
        /// </summary>
        bool ReadOnly { get; }

        /// <summary>
        /// Configuration object generated for this source.
        /// Should be null if loading the config object fails.
        /// </summary>
        T Config { get; }

        /// <summary>
        /// Invalidate the configuration source, causing the next
        /// read to re-initialize the Config object.
        /// </summary>
        void Invalidate();

        /// <summary>
        /// Save the entire object to the config source.
        /// </summary>
        /// <param name="obj">Object to save.</param>
        /// <exception cref="InvalidOperationException">
        /// The IConfigurationSource does not support write operations.
        /// </exception>
        void Save(T obj);

        /// <summary>
        /// Save parts of the object to the config source.
        /// </summary>
        /// <param name="obj">Object to save.</param>
        /// <param name="propertyNames">
        /// List of property names that need saving.
        /// </param>
        /// <exception cref="InvalidOperationException">
        /// The IConfigurationSource does not support write operations.
        /// </exception>
        void PartialSave(T obj, IEnumerable<string> propertyNames);
    }
}
