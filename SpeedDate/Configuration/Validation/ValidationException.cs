using System;

namespace SpeedDate.Configuration.Validation
{
    /// <summary>
    /// Exception indicating that an object failed to validate.
    /// </summary>
    public class ValidationException : Exception
    {
        /// <summary>
        /// New generic validation exception.
        /// </summary>
        public ValidationException()
        {
        }

        /// <summary>
        /// New validation exception with a message
        /// describing why validation failed.
        /// </summary>
        /// <param name="message"></param>
        public ValidationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// New validation exception with a message
        /// describing why validation failed and an
        /// inner exception with more detail.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public ValidationException(string message, Exception innerException)
            :  base(message, innerException)
        {
        }
    }
}
