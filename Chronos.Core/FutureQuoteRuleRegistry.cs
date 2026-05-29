using System;
using System.Collections.Generic;
using System.Linq;

namespace Chronos.Core;

public record FutureQuoteRuleDefinition(string RuleType, Dictionary<string, object> Parameters);

/// <inheritdoc />
public class FutureQuoteRuleRegistry(IEnumerable<IFutureQuoteRuleFactory> factories) : IFutureQuoteRuleRegistry
{
    private readonly Dictionary<string, IFutureQuoteRuleFactory> _factories = 
        factories.ToDictionary(x => x.RuleType);

    /// <inheritdoc/>
    public IFutureQuoteRule Create(string ruleType, Dictionary<string, object> parameters)
    {
        if (_factories.TryGetValue(ruleType, out var factory))
            return factory.Create(parameters);
        
        throw new ArgumentException($"No factory found for rule type '{ruleType}'");
    }

    /// <inheritdoc/>
    public FutureQuoteRuleDefinition ToDefinition(IFutureQuoteRule rule)
    {
        if (!_factories.ContainsKey(rule.RuleType))
            throw new ArgumentException($"No factory found for rule type '{rule.RuleType}'");

        return new FutureQuoteRuleDefinition(rule.RuleType, rule.ToDictionary());
    }
}