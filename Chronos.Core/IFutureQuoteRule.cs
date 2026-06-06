using System.Collections.Generic;
using ZES.Interfaces.Clocks;

namespace Chronos.Core;

/// <summary>
/// Represents a rule used to provide the future quotes during a request operation.
/// </summary>
public interface IFutureQuoteRule
{
    /// <summary>
    /// Gets the type of the rule, uniquely identifying its purpose or behavior.
    /// </summary>
    string RuleType { get; }

    /// <summary>
    /// Determines whether the current rule applies to the specified quote data request.
    /// </summary>
    /// <param name="request">The data request for which the applicability of this rule is being determined.</param>
    /// <returns>
    /// True if the rule applies to the provided <paramref name="request"/>; otherwise, false.
    /// </returns>
    bool AppliesTo(QuoteDataRequest request);

    /// <summary>
    /// Applies the future quote rule to a given quote data request and calculates the resulting quote value.
    /// </summary>
    /// <param name="request">The data request containing information about the asset, timestamps, and other parameters.</param>
    /// <param name="currentQuote">The current quote value to which the rule will be applied.</param>
    /// <returns>The resulting quote value after applying the rule.</returns>
    public IReadOnlyDictionary<Time, double> Apply(QuoteDataRequest request, double currentQuote);

    /// <summary>
    /// Converts the future quote rule into a dictionary representation of its parameters.
    /// </summary>
    /// <returns>A dictionary containing the parameters of the future quote rule, with parameter names as keys and their corresponding values.</returns>
    public Dictionary<string, object> ToDictionary();
}