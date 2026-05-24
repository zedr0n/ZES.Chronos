using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using NodaTime;
using ZES.Infrastructure.Utils;

namespace Chronos.Core;

/// <summary>
/// Provides extension methods for handling quote-related operations.
/// </summary>
[SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1010:Opening square brackets should be spaced correctly", Justification = "[] issue")]
public static class QuoteExtensions
{
    /// <summary>
    /// Determines whether a quote already exists for a given timestamp in the provided quote data.
    /// </summary>
    /// <param name="timestamp">
    /// The timestamp for which the existence of a quote is to be checked.
    /// </param>
    /// <param name="quoteDates">
    /// A set of exact quote timestamps.
    /// </param>
    /// <param name="effectiveQuoteDates">
    /// A dictionary containing quote timestamps grouped by their respective local dates after rolling
    /// them to a working day based on the specified holiday calendar.
    /// </param>
    /// <param name="intraday">
    /// A flag indicating whether intraday quotes should always be updated.
    /// </param>
    /// <param name="holidayCalendar">
    /// The holiday calendar used for evaluating working days.
    /// </param>
    /// <returns>
    /// True if a quote already exists for the given timestamp or if conditions are met based on intraday flag
    /// and explicit time; otherwise, false.
    /// </returns>
    public static bool HasExistingQuote(Instant timestamp, ISet<Instant> quoteDates, Dictionary<LocalDate, List<Instant>> effectiveQuoteDates, bool intraday, string holidayCalendar)
    {
        var hasExactQuote = quoteDates?.Contains(timestamp) ?? false;
        if (hasExactQuote)
            return true;

        effectiveQuoteDates ??= quoteDates?
            .GroupBy(d => d.RollToWorkingDay(holidayCalendar: holidayCalendar).Date())
            .ToDictionary(g => g.Key, g => g.ToList()) ?? [];
        
        var hasSameDayQuote = effectiveQuoteDates.ContainsKey(timestamp.RollToWorkingDay(holidayCalendar).Date());

        var hasExplicitTime = timestamp != timestamp.StartOfDay();
        if (!timestamp.IsWorkingDay(holidayCalendar))
            hasExplicitTime = false;

        // we always try to update quote for intraday if requested
        return hasSameDayQuote && !intraday && !hasExplicitTime;
    }
}