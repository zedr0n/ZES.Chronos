using System.Collections.Generic;

namespace Chronos.Core;

/// <summary>
/// Factory class responsible for creating instances of <see cref="FutureQuoteRuleFixed"/>.
/// Implements the <see cref="IFutureQuoteRuleFactory"/> interface and provides a mechanism
/// to produce fixed quote rules with predefined parameters.
/// </summary>
public class FutureQuoteRuleFixedFactory : IFutureQuoteRuleFactory
{
    /// <inheritdoc/>
    public string RuleType { get; } = "Fixed";
    
    /// <inheritdoc/>
    public IFutureQuoteRule Create(Dictionary<string, object> parameters) 
        => new FutureQuoteRuleFixed((double)parameters["Quote"]);
}