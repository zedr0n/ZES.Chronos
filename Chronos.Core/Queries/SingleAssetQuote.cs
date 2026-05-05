using Newtonsoft.Json;
using NodaTime;
using ZES.Infrastructure.Utils;
using ZES.Interfaces.Domain;

namespace Chronos.Core.Queries;

/// <summary>
/// Represents a quote for a single asset with its price, timestamp, and additional metadata.
/// </summary>
[method: JsonConstructor]
public class SingleAssetQuote(double price, Instant timestamp, string holidayCalendar = null, bool supportsIntraday = true) : ISingleState
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SingleAssetQuote"/> class.
    /// </summary>
    public SingleAssetQuote()
        : this(double.NaN, Instant.MinValue) { }
    
    /// <summary>
    /// Gets the price of a single asset quote in the financial market.
    /// </summary>
    /// <remarks>
    /// This property provides the quoted price for a particular asset at a specified point in time.
    /// It is part of the <c>SingleAssetQuote</c> class, which encapsulates information regarding
    /// asset quotations.
    /// </remarks>
    public double Price => price;

    /// <summary>
    /// Gets the timestamp associated with the quote.
    /// The timestamp represents the specific point in time when the price
    /// for the asset was recorded or evaluated. It is expressed as an
    /// <see cref="NodaTime.Instant"/> and is crucial for understanding the temporal
    /// context of the asset's pricing data.
    /// </summary>
    public Instant Timestamp => timestamp;

    /// <summary>
    /// Gets a value indicating whether the quote supports intraday price tracking.
    /// </summary>
    /// <remarks>
    /// This property specifies if the quote allows for detailed price tracking within the same trading day.
    /// It is useful in scenarios where high-frequency or intraday data is required to make trading decisions.
    /// </remarks>
    public bool SupportsIntraday => supportsIntraday;

    /// <summary>
    /// Gets the identifier or description of the holiday calendar associated with the asset quote.
    /// </summary>
    /// <remarks>
    /// This property indicates the calendar that specifies non-working days, holidays, or market-closed days
    /// relevant to the asset's trading or pricing. It is used to determine working days for asset operations
    /// such as validation or calculations involving time-based constraints.
    /// </remarks>
    public string HolidayCalendar => holidayCalendar;

    /// <summary>
    /// Validates if the current quote is valid based on its timestamp and price value,
    /// considering fallback conditions and intraday constraints.
    /// </summary>
    /// <param name="currentTimestamp">The current timestamp to validate the quote against.</param>
    /// <param name="intraday">A flag indicating whether to consider intraday conditions for validation.</param>
    /// <returns>Returns true if the quote is valid; otherwise, false.</returns>
    /// <remarks>
    /// If the quote is a fallback quote, it is considered valid if it is within the last 2 working days (inclusive).
    /// Timestamps are also validated based on whether intraday constraints are enforced.
    /// </remarks>
    public bool IsValid(Instant currentTimestamp, bool intraday = false)
    {
        var b = !double.IsNaN(Price);
        if (!b)
            return false;

        if (holidayCalendar == null)
        {
            b &= Timestamp.IsSameDay(currentTimestamp);
        }
        else
        {
            var numberOfWorkingDays = 1;
            if (intraday && !SupportsIntraday)
                numberOfWorkingDays++;

            b &= Timestamp.IsWithinPriorWorkingDays(currentTimestamp, numberOfWorkingDays);
        }

        return b;
    }
}