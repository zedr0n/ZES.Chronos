using System.Collections.Generic;

namespace Chronos.Core;

/// <summary>
/// Represents a fixed future quote rule, implementing the IFutureQuoteRule interface.
/// This rule always returns a predefined fixed quote value regardless of the request.
/// </summary>
public class FutureQuoteRuleFixed(double quote) : IFutureQuoteRule
{
    /// <inheritdoc/>
    public string RuleType { get; } = "Fixed";
    
    /// <inheritdoc/>
    public bool AppliesTo(QuoteDataRequest request) => true;

    /// <inheritdoc/>
    public double Apply(QuoteDataRequest request, double currentQuote) => quote;

    /// <inheritdoc/>
    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            ["Quote"] = quote,
        };
    }
}