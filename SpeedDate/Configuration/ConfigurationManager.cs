using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using SpeedDate.Configuration.Sources;
using SpeedDate.Configuration.Validation;

namespace SpeedDate.Configuration
{
    /// <summary>
    /// A configuration manager that loads and merges settings from
    /// multiple sources and intelligently tracks changes to the
    /// resulting configuration object.
    /// </summary>
    /// <typeparam name="T">Strongly typed Configuration object</typeparam>
    public class ConfigurationManager<T> where T : class, new()
    {
        /// <summary>
        /// Whether or not change tracking is currently enabled.
        /// </summary>
        public bool ChangeTrackingEnabled { get; private set; }

        private IConfigurationSource<T>[] ConfigStack { get; set; }

        private readonly IConfigurationSource<T> _primarySource;

        private readonly HashSet<string> _propertiesAlwaysSerialized;

        private readonly HashSet<string> _propertiesNeverSerialized;

        /// <summary>
        /// This object contains the merged configuration settings.
        /// It will track all changes and save them out to a file
        /// when <see ref="SaveChanges"/> is called.
        /// </summary>
        public T Out { get; private set; }

        /// <summary>
        /// Create a ConfigurationManager loading its information
        /// from a list of XML configuration files parsed by
        /// <see cref="XmlFileConfigurationSource{T}"/> sources.
        /// Convenience method when only using XML files for storage.
        /// The final file in the list will be used as the primary source.
        /// </summary>
        /// <param name="configFiles">Filenames to load.</param>
        public ConfigurationManager(params string[] configFiles)
            : this(configFiles.Select(c =>
                (IConfigurationSource<T>)new XmlFileConfigurationSource<T>(c))
                .ToArray())
        {
        }

        /// <summary>
        /// Create a ConfigurationManager, loading its information
        /// from an ordered list of sources.
        /// 
        /// If no source is marked as a PrimarySource, the last
        /// source in the sequence will be assumed to be primary.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// More than one source is marked as a PrimarySource.
        /// </exception>
        /// <param name="sources"></param>
        public ConfigurationManager(params IConfigurationSource<T>[] sources)
            : this(null, sources)
        {
        }

        /// <summary>
        /// Create a ConfigurationManager, loading its information
        /// from an ordered list of sources.
        /// 
        /// If no source is marked as a PrimarySource, the last
        /// source in the sequence will be assumed to be primary.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// More than one source is marked as a PrimarySource.
        /// </exception>
        /// <param name="validator">
        /// Validates each source's output. If null,
        /// validation is not performed.
        /// </param>
        /// <param name="sources"></param>
        public ConfigurationManager(IValidator<T> validator, params IConfigurationSource<T>[] sources)
        {
            _propertiesAlwaysSerialized = new HashSet<string>();
            _propertiesNeverSerialized = new HashSet<string>();

            var primarySources = sources.Where(s => s.PrimarySource);
            if (primarySources.Count() > 1)
            {
                throw new ArgumentException("Cannot have more than one primary source.");
            }
            _primarySource = primarySources.Any() ?
                primarySources.First() :
                sources.Last();

            var workingSources = new List<IConfigurationSource<T>>();
            Out = new T();
            foreach (var source in sources)
            {
                T config = null;

                // ReSharper disable EmptyGeneralCatchClause
                try
                {
                    config = source.Config;
                }
                catch (Exception)
                {
                    // Ignore errors if source is not required...
                }
                // ReSharper restore EmptyGeneralCatchClause

                if (config == null && source.Required)
                {
                    throw new InvalidDataException(String.Format(
                        "Required source {0} failed to create a config file.", source));
                }
                
                Out.MergeWith(config);
                workingSources.Add(source);
            }

            if (validator != null)
            {
                validator.Validate(Out);
            }

            ConfigStack = workingSources.ToArray();

            EnableChangeTracking();
        }

        /// <summary>
        /// Tell the configuration manager to always serialize this property.
        /// If the property is currently marked as "never serialize", that
        /// status will be removed.
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="prop"></param>
        /// <returns></returns>
        public ConfigurationManager<T> AlwaysSerialize<TProp>(Expression<Func<T, TProp>> prop)
        {
            var name = ((MemberExpression) prop.Body).Member.Name;
            _propertiesAlwaysSerialized.Add(name);
            _propertiesNeverSerialized.Remove(name);
            return this;
        }

        /// <summary>
        /// Tell the configuration manager to never serialize this property.
        /// If the property is currently marked as "always serialize", that
        /// status will be removed.
        /// </summary>
        /// <typeparam name="TProp"></typeparam>
        /// <param name="prop"></param>
        /// <returns></returns>
        public ConfigurationManager<T> NeverSerialize<TProp>(Expression<Func<T, TProp>> prop)
        {
            var name = ((MemberExpression)prop.Body).Member.Name;
            _propertiesNeverSerialized.Add(name);
            _propertiesAlwaysSerialized.Remove(name);
            return this;
        }

        /// <summary>
        /// Return a configuration object composed of
        /// all sources except the primary source
        /// merged into one.
        /// </summary>
        /// <returns></returns>
        private T CreateBaseConfig()
        {
            return ConfigStack
                .Select(c => c.Config)
                .Where(c => !ReferenceEquals(c, _primarySource.Config)).Merge();
        }

        /// <summary>
        /// Temporarily disables change tracking.
        /// </summary>
        public void DisableChangeTracking()
        {
            if (!ChangeTrackingEnabled) return;

            throw new NotImplementedException();
        }

        /// <summary>
        /// Enables change tracking after it has
        /// been disabled.
        /// </summary>
        public void EnableChangeTracking()
        {
            if (!ChangeTrackingEnabled)
            {
                ChangeTrackingEnabled = true;
            }
        }

        /// <summary>
        /// Save changes to <see cref="Out"/> to
        /// the PrimarySource.
        /// </summary>
        public void SaveChanges()
        {
            SaveChanges(_primarySource);
        }

        /// <summary>
        /// Saves changes to <see cref="Out"/> to the
        /// given IConfigurationSource.
        /// </summary>
        /// <param name="source">Source to use as the primary source.</param>
        public void SaveChanges(IConfigurationSource<T> source)
        {
            var @base = CreateBaseConfig();
            var mrg = new T();
            mrg.MergeWith(_primarySource.Config);
            mrg.MergeWith(Out, defaultObject: @base);

            // Copy over the values for all "always serialized" properties
            foreach (var prop in _propertiesAlwaysSerialized
                .Select(propName => typeof (T).GetProperty(propName)))
            {
                prop.SetValue(mrg, prop.GetValue(Out, null), null);
            }

            source.PartialSave(mrg,
                GetProperties(@base, _primarySource.Config, PropertyStatus.Changed)
                    .Select(p => p.Name)
                    .Concat(_propertiesAlwaysSerialized)
                    .Except(_propertiesNeverSerialized));
        }

        /// <summary>
        /// Retrieves the names of all properties that
        /// have been changed since loading.
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, object> GetPropertyChangesByName()
        {
            var @base = CreateBaseConfig();
            var primaryChanges = GetProperties(@base, _primarySource.Config, PropertyStatus.Changed)
                .ToDictionary(
                    k => k.Name,
                    v => v.GetGetMethod().Invoke(_primarySource.Config, null));
            var outChanges = GetProperties(@base, Out, PropertyStatus.Changed)
                .ToDictionary(
                    k => k.Name,
                    v => v.GetGetMethod().Invoke(Out, null));

            var finalChanges = new Dictionary<string, object>(primaryChanges);

            foreach (var outChange in outChanges)
            {
                finalChanges[outChange.Key] = outChange.Value;
            }

            return finalChanges;
        }

        private enum PropertyStatus
        {
            Changed,
            Unchanged
        }

        private static IEnumerable<PropertyInfo> GetProperties(T start, T end, PropertyStatus status)
        {
            var @default = new T();
            foreach (var info in typeof(T).GetProperties())
            {
                var priValue = info.GetGetMethod().Invoke(end, null);
                var baseValue = info.GetGetMethod().Invoke(start, null);
                var defaultValue = info.GetGetMethod().Invoke(@default, null);

                var propertyChanged = !Equals(priValue, baseValue) && 
                    !Equals(priValue, defaultValue);
                if (status == PropertyStatus.Changed && propertyChanged)
                {
                    yield return info;
                }
                else if(status == PropertyStatus.Unchanged && !propertyChanged)
                {
                    yield return info;
                }
            }
        }
    }
}