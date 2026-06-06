using System.Collections.Generic;

namespace Chronos.Core;

/// <summary>
/// Factory class responsible for creating instances of <see cref="FutureQuoteRuleConstantGrowth"/>.
/// Implements the <see cref="IFutureQuoteRuleFactory"/> interface, allowing the creation of
/// constant growth quote rules using specified parameters.
/// </summary>
public class FutureQuoteRuleConstantGrowthFactory : IFutureQuoteRuleFactory
{
    /// <inheritdoc/>
    public string RuleType { get; } = "ConstantGrowth";

    /// <inheritdoc/>
    public IFutureQuoteRule Create(Dictionary<string, object> parameters)
        => new FutureQuoteRuleConstantGrowth((double)parameters["Rate"]);
}