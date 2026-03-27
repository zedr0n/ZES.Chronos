using ZES.Infrastructure.Domain;

namespace Chronos.Core.Events;

/// <summary>
/// Represents an event that is triggered when a quote ticker is added.
/// </summary>
/// <remarks>
/// The <see cref="QuoteTickerAdded"/> event carries the ticker value
/// associated with a quote in an asset pair. This event signifies that
/// a new ticker has been registered for tracking within the system.
/// </remarks>
/// <param name="ticker">The symbol or unique identifier for the quote ticker being added.</param>
public class QuoteTickerAdded(string ticker) : Event
{
    /// <summary>
    /// Initializes a new instance of the <see cref="QuoteTickerAdded"/> class.
    /// </summary>
    public QuoteTickerAdded()
        : this(string.Empty) { }

    /// <summary>
    /// Gets the ticker associated with the asset pair.
    /// </summary>
    /// <remarks>
    /// The ticker represents the symbol or unique identifier that is used
    /// to track quotes for a specific asset pair. It is typically added
    /// or updated when a <see cref="QuoteTickerAdded"/> event is applied
    /// to the asset pair.
    /// This property is used to identify and query ticker-specific data
    /// in the context of financial assets.
    /// </remarks>
    public string Ticker => ticker;
}