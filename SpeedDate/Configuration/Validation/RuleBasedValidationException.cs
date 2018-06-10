using System;

namespace SpeedDate.Configuration.Validation
{
    /// <summary>
    /// Thrown when validation fails.
    /// </summary>
    public sealed class RuleBasedValidationException : ValidationException
    {
        /// <summary>
        /// Create a new validation failure with the name of
        /// the rule that failed and a message explaining why.
        /// </summary>
        /// <param name="message"></param>
        public RuleBasedValidationException(string message)
            : base(message)
        {
        }

        /// <summary>
        /// Create a new validation failure with the name of
        /// the rule that failed, a message explaining why,
        /// and an exception with more information.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public RuleBasedValidationException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
