using System.Collections.Generic;
using System.Linq;
using ZES.Interfaces.Clocks;

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
    public IReadOnlyDictionary<Time, double> Apply(QuoteDataRequest request, double currentQuote) => 
        request.Timestamps.ToDictionary(t => t, t => quote);

    /// <inheritdoc/>
    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>
        {
            ["Quote"] = quote,
        };
    }
}