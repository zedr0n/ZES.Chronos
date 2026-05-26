using System;
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
    /// <param name="supportsIntraday">A flag specifying whether intraday quotes are supported.</param>
    /// <param name="holidayCalendar">
    /// The holiday calendar used for evaluating working days.
    /// </param>
    /// <returns>
    /// True if a quote already exists for the given timestamp or if conditions are met based on intraday flag
    /// and explicit time; otherwise, false.
    /// </returns>
    public static bool HasExistingQuote(Instant timestamp, ISet<Instant> quoteDates, Dictionary<LocalDate, List<Instant>> effectiveQuoteDates, bool intraday, bool supportsIntraday, string holidayCalendar)
    {
        var hasExactQuote = quoteDates?.Contains(timestamp) ?? false;
        if (hasExactQuote)
            return true;
        
        intraday &= timestamp.IsWorkingDay(holidayCalendar);

        effectiveQuoteDates ??= quoteDates?
            .GroupBy(d => d.RollToWorkingDay(holidayCalendar: holidayCalendar).Date())
            .ToDictionary(g => g.Key, g => g.ToList()) ?? [];
        
        return GetCandidateDates(timestamp, intraday, supportsIntraday, holidayCalendar)
            .Distinct()
            .SelectMany(date => effectiveQuoteDates.TryGetValue(date, out var candidates) ? candidates : [])
            .Any(t => IsValidQuoteFor(timestamp, t, intraday, supportsIntraday, holidayCalendar));
    }

    /// <summary>
    /// Determines whether a given quote timestamp is valid for the provided timestamp
    /// based on intraday flags and holiday calendar rules.
    /// </summary>
    /// <param name="timestamp">
    /// The reference timestamp for which to validate the quote.
    /// </param>
    /// <param name="quoteTimestamp">
    /// The timestamp of the quote being validated.
    /// </param>
    /// <param name="intraday">
    /// A flag indicating whether intraday quotes should always be updated.
    /// </param>
    /// <param name="supportsIntraday">A flag specifying whether intraday quotes are supported.</param>
    /// <param name="holidayCalendar">
    /// The holiday calendar used to determine working day rules.
    /// </param>
    /// <returns>
    /// True if the quote timestamp is valid for the provided timestamp based on the
    /// specified conditions; otherwise, false.
    /// </returns>
    public static bool IsValidQuoteFor(Instant timestamp, Instant quoteTimestamp, bool intraday, bool supportsIntraday, string holidayCalendar)
    {
        if (timestamp == quoteTimestamp)
            return true;

        var isWorkingDay = timestamp.IsWorkingDay(holidayCalendar);
        intraday &= isWorkingDay;
        
        var hasExplicitTime = timestamp != timestamp.StartOfDay() && isWorkingDay;
        
        var effectiveQuoteDate = quoteTimestamp.RollToWorkingDay(holidayCalendar).Date();
        var sameDayQuote = timestamp.RollToWorkingDay(holidayCalendar).Date() == effectiveQuoteDate;
        if (intraday && !supportsIntraday && !sameDayQuote)
            sameDayQuote = timestamp.PreviousWorkingDay(holidayCalendar).Date() == effectiveQuoteDate;

        if (intraday)
            return supportsIntraday ? IsWithinSeconds(timestamp, quoteTimestamp) : sameDayQuote;
        
        return sameDayQuote && !hasExplicitTime;
    }

    /// <summary>
    /// Gets candidate dates based on the provided timestamp and additional context, such as intraday settings,
    /// support for intraday quotes, and the specified holiday calendar.
    /// </summary>
    /// <param name="timestamp">
    /// The timestamp used as the basis for determining candidate dates.
    /// </param>
    /// <param name="intraday">
    /// A flag indicating whether intraday quotes should be considered.
    /// </param>
    /// <param name="supportsIntraday">
    /// A flag specifying whether intraday quotes are supported.
    /// </param>
    /// <param name="holidayCalendar">
    /// The holiday calendar used for evaluating working and non-working days.
    /// </param>
    /// <returns>
    /// A collection of candidate dates that are determined based on the timestamp and contextual parameters.
    /// </returns>
    private static IEnumerable<LocalDate> GetCandidateDates(Instant timestamp, bool intraday, bool supportsIntraday, string holidayCalendar)
    {
        var isWorkingDay = timestamp.IsWorkingDay(holidayCalendar);
        if (intraday && supportsIntraday && isWorkingDay)
            yield break;
            
        yield return timestamp.RollToWorkingDay(holidayCalendar).Date();

        if (holidayCalendar == null)
            yield break;

        if (intraday && !supportsIntraday && isWorkingDay)
            yield return timestamp.PreviousWorkingDay(holidayCalendar).Date();
    }

    /// <summary>
    /// Determines whether the difference between two timestamps is within a specified number of seconds.
    /// </summary>
    /// <param name="a">
    /// The first timestamp to be compared.
    /// </param>
    /// <param name="b">
    /// The second timestamp to be compared.
    /// </param>
    /// <param name="seconds">
    /// The allowable difference in seconds between the two timestamps. Defaults to 60 seconds.
    /// </param>
    /// <returns>
    /// True if the difference between the two timestamps is less than the specified number of seconds; otherwise, false.
    /// </returns>
    private static bool IsWithinSeconds(Instant a, Instant b, int seconds = 60)
        => Math.Abs((a - b).TotalSeconds) < seconds;
}