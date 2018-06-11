namespace SpeedDate.Configuration.SmartConf.Validation
{
    /// <summary>
    /// Validate an object.
    /// </summary>
    /// <typeparam name="T">Type to validate.</typeparam>
    public interface IValidator<in T>
    {
        /// <summary>
        /// Attempt to validate the object and throw a
        /// <see cref="ValidationException"/> if it fails.
        /// </summary>
        /// <exception cref="ValidationException">
        /// A rule failed to validate.
        /// </exception>
        /// <param name="obj">Object to validate.</param>
        void Validate(T obj);
    }
}
