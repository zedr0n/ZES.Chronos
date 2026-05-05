using Newtonsoft.Json;
using NodaTime;
using ZES.Interfaces.Domain;

namespace Chronos.Core.Queries;

/// <summary>
/// Represents information about an asset pair, including the assets
/// involved, relevant quote dates, and associated ticker information.
/// </summary>
[method: JsonConstructor]
public class AssetPairInfo(Asset forAsset, Asset domAsset, Instant[] quoteDates, string ticker, string holidayCalendar = null, bool supportsIntraday = true) : ISingleState
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AssetPairInfo"/> class.
    /// </summary>
    public AssetPairInfo()
        : this(null, null, new Instant[] { }, null) { }
    
    /// <summary>
    /// Gets the asset that represents the base or primary asset in the asset pair.
    /// </summary>
    /// <remarks>
    /// The property identifies the "For" asset in the asset pair relationship,
    /// distinguishing it from the "Domestic" (or "Dom") asset. This asset
    /// serves as the base currency or primary context in financial calculations
    /// involving the asset pair, such as determining exchange rates or other
    /// relationships.
    /// </remarks>
    public Asset ForAsset => forAsset;

    /// <summary>
    /// Gets the dominant or quote asset in the asset pair.
    /// </summary>
    /// <remarks>
    /// This property represents the asset in the pair that serves as the quote currency or dominant asset,
    /// which is commonly used for pricing and valuation purposes. It is an instance of the <see cref="Asset"/>
    /// class and is part of the <see cref="AssetPairInfo"/> object.
    /// </remarks>
    public Asset DomAsset => domAsset;

    /// <summary>
    /// Gets the array of dates when quotes for a specific asset pair are available.
    /// Each date corresponds to a point in time where a quote has been recorded for the
    /// asset pair, providing historical data for tracking and analysis.
    /// </summary>
    public Instant[] QuoteDates => quoteDates;

    /// <summary>
    /// Gets the ticker symbol associated with the asset pair.
    /// The ticker is a unique identifier typically used to specify the trading pair
    /// across financial markets or systems.
    /// </summary>
    public string Ticker => ticker;

    /// <summary>
    /// Gets a value indicating whether intraday trading or operations are supported for the asset pair.
    /// </summary>
    /// <remarks>
    /// This property specifies the availability of intraday activities, such as trading,
    /// price updates, or other operations, within the context of the associated asset pair.
    /// If set to <c>true</c>, the asset pair allows such activities to occur within
    /// the span of a single trading day; otherwise, it does not.
    /// </remarks>
    public bool SupportsIntraday => supportsIntraday;

    /// <summary>
    /// Gets the name of the holiday calendar associated with the asset pair.
    /// </summary>
    /// <remarks>
    /// The property specifies the holiday calendar used to define non-working days
    /// or holidays that impact the trading or settlement of the asset pair.
    /// This may be particularly relevant for financial calculations involving
    /// schedules or date adjustments.
    /// </remarks>
    public string HolidayCalendar { get; } = holidayCalendar;
}