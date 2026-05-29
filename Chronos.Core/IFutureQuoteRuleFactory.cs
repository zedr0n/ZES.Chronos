using System.Collections.Generic;

namespace Chronos.Core;

/// <summary>
/// Defines a contract for a factory that creates instances of future quote rules based on specified parameters.
/// </summary>
public interface IFutureQuoteRuleFactory
{
    /// <summary>
    /// Gets the type of the rule represented by this object.
    /// This property is used as a unique identifier to associate
    /// the rule with corresponding factories and registries.
    /// </summary>
    string RuleType { get; }

    /// <summary>
    /// Creates a new instance of an object implementing the <see cref="IFutureQuoteRule"/> interface
    /// using the specified parameters.
    /// </summary>
    /// <param name="parameters">
    /// A dictionary containing the parameters required to create the future quote rule.
    /// </param>
    /// <returns>
    /// An instance of an object implementing the <see cref="IFutureQuoteRule"/> interface.
    /// </returns>
    IFutureQuoteRule Create(Dictionary<string, object> parameters);
}