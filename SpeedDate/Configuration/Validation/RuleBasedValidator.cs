using System.Collections.Generic;

namespace SpeedDate.Configuration.Validation
{
    /// <summary>
    /// Rule where validation status is based 
    /// on the boolean value returned.
    /// </summary>
    /// <typeparam name="T">Type being validated.</typeparam>
    /// <param name="obj">Object being validated.</param>
    /// <returns>True if validated, false otherwise.</returns>
    public delegate bool BooleanRule<in T>(T obj);

    /// <summary>
    /// Complex rules are assumed to validate unless
    /// they throw a <see cref="RuleBasedValidationException"/>.
    /// </summary>
    /// <exception cref="RuleBasedValidationException">
    /// Exception explaining why a rule did not validate.
    /// </exception>
    /// <typeparam name="T">Type to validate.</typeparam>
    /// <param name="obj">Object to validate.</param>
    public delegate void ComplexRule<in T>(T obj);

    /// <summary>
    /// Validate an object based on a set of rules.
    /// </summary>
    /// <typeparam name="T">Type to validate.</typeparam>
    public sealed class RuleBasedValidator<T> : IValidator<T>
    {
        private class RuleMetadata
        {
            public string Message { get; set; }

            public BooleanRule<T> Rule { get; set; }
        }

        private readonly List<RuleMetadata> _rules;
 
        /// <summary>
        /// Create a new validator.
        /// </summary>
        public RuleBasedValidator()
        {
            _rules = new List<RuleMetadata>();
        }

        /// <summary>
        /// Add a new unnamed boolean rule to the list
        /// of validation rules.
        /// </summary>
        /// <param name="rule">Validation rule to add.</param>
        public void AddRule(BooleanRule<T> rule)
        {
            AddRule(rule, "Validation failed.");
        }

        /// <summary>
        /// Add a new named boolean rule to the list
        /// of validation rules.
        /// </summary>
        /// <param name="rule">Validation rule to add.</param>
        /// <param name="message">Message explaining the rule.</param>
        public void AddRule(BooleanRule<T> rule, string message)
        {
            _rules.Add(new RuleMetadata
            {
                Message = message,
                Rule = rule
            });
        }

        /// <summary>
        /// Add a named complex rule.
        /// Complex rules are assumed to validate unless
        /// they throw a <see cref="RuleBasedValidationException"/>.
        /// </summary>
        /// <param name="rule">Validation rule to add.</param>
        public void AddRule(ComplexRule<T> rule)
        {
            _rules.Add(new RuleMetadata
            {
                Rule = obj => { rule(obj); return true; }
            });
        }

        public void Validate(T obj)
        {
            foreach (var rule in _rules)
            {
                if (!rule.Rule(obj))
                {
                    throw new RuleBasedValidationException(rule.Message);
                }
            }
        }
    }
}
