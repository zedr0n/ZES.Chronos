using System.Collections.Generic;

namespace Chronos.Core;

/// <summary>
/// Represents a flat rule for providing future quote data.
/// This rule returns the current quote value without any modification. 
/// </summary>
public class FutureQuoteRuleFlat() : IFutureQuoteRule
{
    /// <inheritdoc/>
    public string RuleType { get; } = "Flat";

    /// <inheritdoc/>
    public bool AppliesTo(QuoteDataRequest request) => true;

    /// <inheritdoc/>
    public double Apply(QuoteDataRequest request, double currentQuote) => currentQuote;

    /// <inheritdoc/>
    public Dictionary<string, object> ToDictionary() => new();
}