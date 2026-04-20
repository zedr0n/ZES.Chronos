 using Newtonsoft.Json;

 namespace Chronos.Core.Commands;

 /// <summary>
 /// Represents a command to add a quote for a specific currency pair or asset.
 /// </summary>
 /// <remarks>
 /// The AddQuote class is used to register a financial quote with details such as
 /// the date of the quote, the closing price, and optionally the opening,
 /// lowest, and highest prices. Quotes are associated with a specific target,
 /// indicated by the "fordom" parameter.
 /// </remarks>
 /// <param name="fordom">
 /// The identifier for the target for which the quote is being added. Typically represents a currency pair or asset.
 /// </param>
 /// <param name="date">
 /// The date and time at which the quote is recorded, represented as a NodaTime.Instant.
 /// </param>
 /// <param name="close">
 /// The closing price of the target at the specified date.
 /// </param>
 /// <param name="open">
 /// Optional parameter representing the opening price of the target at the specified date. Default value is 0.
 /// </param>
 /// <param name="low">
 /// Optional parameter representing the lowest price of the target at the specified date. Default value is 0.
 /// </param>
 /// <param name="high">
 /// Optional parameter representing the highest price of the target at the specified date. Default value is 0.
 /// </param>
 [method: JsonConstructor]
 public class AddQuote(string fordom, NodaTime.Instant date, double close, double open = 0, double low = 0, double high = 0) : ZES.Infrastructure.Domain.Command
 {
     /// <summary>
     /// Gets the date associated with the quote.
     /// Represents the exact point in time when the quote was recorded.
     /// </summary>
     public NodaTime.Instant Date => date;

     /// <summary>
     /// Gets the closing value of the quote.
     /// Represents the end-of-period price for the given timestamp.
     /// </summary>
     public double Close => close;

     /// <summary>
     /// Gets the opening price of the quote.
     /// Indicates the initial trading price of an asset for a specific date.
     /// </summary>
     public double Open => open;

     /// <summary>
     /// Gets the lowest recorded value of the quote.
     /// Represents the minimum value during the specified time period for the quote.
     /// </summary>
     public double Low => low;

     /// <summary>
     /// Gets the highest value of the quote within the specified time frame.
     /// Represents the peak price achieved during the recorded period.
     /// </summary>
     public double High => high;

     /// <summary>
     /// Gets or sets a value indicating whether the quote is considered a fallback.
     /// Represents a scenario where the quote was not derived from primary or preferred data sources
     /// and is instead sourced as a backup or alternative value.
     /// </summary>
     public bool IsFallback { get; set; } = false;
     
    /// <inheritdoc/>
     public override string Target => fordom;
 }