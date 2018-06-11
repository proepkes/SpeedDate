using System;
using System.Collections.Generic;
using System.Reflection;

namespace SpeedDate.Configuration.SmartConf
{
    /// <summary>
    ///     Extension class for merging objects.
    /// </summary>
    public static class MergeObjectExtensions
    {
        /// <summary>
        ///     Merges all properties in the secondary object
        ///     with those in the primary.
        ///     A property value will be set only if the
        ///     value in <paramref name="secondary" /> is
        ///     different from the value in <paramref name="primary" />
        ///     AND different from the default value (after construction).
        ///     Note: Dynamic default values (like DateTime.Now) may not
        ///     return the same default value for multiple invocations,
        ///     thus may not merge accurately.
        /// </summary>
        /// <typeparam name="T">
        ///     Type to merge. Must have a default constructor that
        ///     sets default property values.
        /// </typeparam>
        /// <param name="primary">Object to overwrite values for.</param>
        /// <param name="secondary">
        ///     Object to merge into <paramref name="primary" />.
        /// </param>
        public static void MergeWith<T>(this T primary, T secondary) where T : new()
        {
            MergeWith(primary, secondary, new T());
        }

        /// <summary>
        ///     Merges all properties in the secondary object
        ///     with those in the primary.
        ///     A property value will be set only if the
        ///     value in <paramref name="secondary" /> is
        ///     different from the value in <paramref name="primary" />
        ///     AND different from the default value (after construction).
        ///     Note: Dynamic default values (like DateTime.Now) may not
        ///     return the same default value for multiple invocations,
        ///     thus may not merge accurately.
        /// </summary>
        /// <typeparam name="T">
        ///     Type to merge. Must have a default constructor that
        ///     sets default property values.
        /// </typeparam>
        /// <param name="primary">Object to overwrite values for.</param>
        /// <param name="secondary">
        ///     Object to merge into <paramref name="primary" />.
        /// </param>
        /// <param name="defaultObject">
        ///     Object used as the "reference" object for checking default values.
        /// </param>
        public static void MergeWith<T>(this T primary, T secondary, T defaultObject)
        {
            MergeUntyped(typeof(T), primary, secondary, defaultObject);
        }

        private static void MergeUntyped(Type objectType, object primary, object secondary, object defaultObject)
        {
            // We need to know whether or not the value is new or from the constructor.
            // Doesn't work on objects that don't implement IEquatable.
            if (Equals(secondary, null)) return;
            if (Equals(primary, null)) throw new ArgumentNullException("primary");

            foreach (var pi in objectType.GetProperties())
            {
                var secValue = pi.GetValue(secondary, null);
                var defaultValue = pi.GetValue(defaultObject, null);
                if (IsSimplePropety(pi))
                {
                    if (!Equals(secValue, defaultValue))
                    {
                        pi.SetValue(primary, secValue, null);
                    }
                }
                else
                {
                    MergeUntyped(pi.PropertyType,
                        pi.GetValue(primary, null),
                        secValue,
                        defaultValue ?? Activator.CreateInstance(pi.PropertyType));
                }
            }
        }

        private static bool IsSimplePropety(PropertyInfo pi)
        {
            var propertyType = pi.PropertyType;
            return propertyType == typeof(string) || propertyType.IsValueType;
        }

        /// <summary>
        /// Merge the properties of a list of objects
        /// into a new object, in order.
        /// </summary>
        /// <typeparam name="T">Type of the result.</typeparam>
        /// <param name="objects">Objects to compress.</param>
        /// <returns>A single object containing the merged results.</returns>
        public static T Merge<T>(this IEnumerable<T> objects) where T : new()
        {
            return objects.Merge(new T());
        }

        /// <summary>
        /// Merge the properties of a list of objects
        /// into a new object, in order.
        /// </summary>
        /// <typeparam name="T">Type of the result.</typeparam>
        /// <param name="objects">Objects to compress.</param>
        /// <param name="seed">Seed value used as the base of the merge.</param>
        /// <returns>A single object containing the merged results.</returns>
        public static T Merge<T>(this IEnumerable<T> objects, T seed) where T : new()
        {
            foreach (var o in objects)
            {
                seed.MergeWith(o);
            }
            return seed;
        }
    }
}