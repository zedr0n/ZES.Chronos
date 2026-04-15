using ZES.Infrastructure.Domain;

namespace Chronos.Core.Events;

/// <summary>
/// Represents an event triggered when a stock split is added.
/// </summary>
/// <remarks>
/// This event contains information about the stock split ratio, allowing the system
/// to adjust for changes in share quantities resulting from the stock split.
/// </remarks>
public class StockSplitAdded(Asset forAsset, Asset domAsset, double ratio) : Event
{
    /// <summary>
    /// Initializes a new instance of the <see cref="StockSplitAdded"/> class.
    /// Event triggered when a stock split is added to a system or context.
    /// </summary>
    /// <remarks>
    /// Contains information about the stock split ratio, enabling the system to adjust
    /// share quantities or related calculations accordingly.
    /// </remarks>
    public StockSplitAdded() 
        : this(null, null, 1.0) { }

    /// <summary>
    /// Gets the ratio of the stock split.
    /// </summary>
    /// <remarks>
    /// The ratio represents the proportional adjustment applied during the stock split.
    /// For example, a ratio of 2.0 indicates a 2-for-1 split, doubling the number of shares.
    /// </remarks>
    public double Ratio => ratio;

    /// <summary>
    /// Gets the asset associated with the stock split.
    /// </summary>
    /// <remarks>
    /// This property represents the specific asset for which the stock split is applied.
    /// It identifies the subject of the stock split operation in the system.
    /// </remarks>
    public Asset ForAsset => forAsset;

    /// <summary>
    /// Gets the domestic asset associated with the stock split.
    /// </summary>
    public Asset DomAsset => domAsset;
}