using System;
using System.Collections.Generic;

namespace Chronos.Core;

/// <summary>
/// Provides functionality for registering and creating future quote rules, as well as defining their attributes.
/// </summary>
public interface IFutureQuoteRuleRegistry
{
    /// <summary>
    /// Creates an instance of an <see cref="IFutureQuoteRule"/> using the specified rule type and parameters.
    /// </summary>
    /// <param name="ruleType">The type of the rule to create.</param>
    /// <param name="parameters">The parameters required to construct the rule.</param>
    /// <returns>An instance of the specified <see cref="IFutureQuoteRule"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when no factory is found for the specified rule type.</exception>
    IFutureQuoteRule Create(string ruleType, Dictionary<string, object> parameters);

    /// <summary>
    /// Gets the definition of the specified <see cref="IFutureQuoteRule"/> as a tuple containing its rule type and parameters.
    /// </summary>
    /// <param name="rule">The <see cref="IFutureQuoteRule"/> instance to convert to a definition.</param>
    /// <returns>A tuple containing the rule type and a dictionary of parameters for the specified <see cref="IFutureQuoteRule"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when no factory is found for the rule type of the specified <see cref="IFutureQuoteRule"/>.</exception>
    FutureQuoteRuleDefinition ToDefinition(IFutureQuoteRule rule);
}