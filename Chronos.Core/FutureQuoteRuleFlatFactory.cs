using System.Collections.Generic;

namespace Chronos.Core;

/// <summary>
/// A factory class responsible for creating instances of the <see cref="FutureQuoteRuleFlat"/> class.
/// Implements the <see cref="IFutureQuoteRuleFactory"/> interface to adhere to the extensible
/// rule-creation workflow for future quote rules.
/// </summary>
public class FutureQuoteRuleFlatFactory : IFutureQuoteRuleFactory
{
    /// <inheritdoc/>
    public string RuleType { get; } = "Flat";

    /// <inheritdoc/>
    public IFutureQuoteRule Create(Dictionary<string, object> parameters) => new FutureQuoteRuleFlat();
}