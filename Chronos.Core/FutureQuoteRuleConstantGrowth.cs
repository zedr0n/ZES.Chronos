using System;
using System.Collections.Generic;
using System.Linq;
using ZES.Interfaces.Clocks;

namespace Chronos.Core;

/// <summary>
/// Represents a rule for forecasting future quotes based on a constant growth rate.
/// Implements the <see cref="IFutureQuoteRule"/> interface to calculate projected quotes
/// using an exponential growth model relative to the specified rate.
/// </summary>
public class FutureQuoteRuleConstantGrowth(double rate) : IFutureQuoteRule
{
    /// <inheritdoc/>
    public string RuleType { get; } = "ConstantGrowth";
    
    /// <inheritdoc/>
    public bool AppliesTo(QuoteDataRequest request) => true;

    /// <inheritdoc/>
    public IReadOnlyDictionary<Time, double> Apply(QuoteDataRequest request, double currentQuote)
    {
        return request.Timestamps.ToDictionary(t => t, t => currentQuote * Math.Pow(1 + rate, Math.Round((t - request.Now).TotalDays, 0) / 365));
    }

    /// <inheritdoc/>
    public Dictionary<string, object> ToDictionary()
    {
        return new Dictionary<string, object>()
        {
            ["Rate"] = rate,
        };
    }
}