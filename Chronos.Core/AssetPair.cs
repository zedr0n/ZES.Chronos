using System.Collections.Generic;
using Chronos.Core.Events;
using NodaTime;

namespace Chronos.Core
{
    /// <summary>
    /// Represents a pair of assets
    /// </summary>
    /// <remarks>
    /// An AssetPair consists of a "foreign" asset and a "domestic" asset, representing
    /// a trading pair or a conversion relationship. The class provides functionality to
    /// register asset pairs, add quotes, and associate a URL for quote information.
    /// </remarks>
    public sealed class AssetPair : ZES.Infrastructure.Domain.AggregateRoot
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AssetPair"/> class.
        /// </summary>
        public AssetPair()
        {
            Register<AssetPairRegistered>(ApplyEvent);
            Register<QuoteAdded>(ApplyEvent);
            Register<QuoteUrlAdded>(ApplyEvent);
            Register<QuoteTickerAdded>(ApplyEvent);
            Register<StockSplitAdded>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetPair"/> class.
        /// Represents a pair of assets.
        /// </summary>
        /// <param name="fordom">Unique identifier for the asset pair</param>
        /// <param name="forAsset">Foreign asset in the pair</param>
        /// <param name="domAsset">Domestic asset in the pair</param>
        /// <param name="supportsIntraday">Indicates whether intraday trading or quoting is supported</param>
        /// <param name="holidayCalendar">Holiday calendar for the asset pair</param>
        /// <remarks>
        /// An AssetPair consists of a "foreign" asset and a "domestic" asset, representing
        /// a trading pair or a conversion relationship. The class provides functionality to
        /// register asset pairs, add quotes, and associate a URL for quote information.
        /// </remarks>
        public AssetPair(string fordom, Asset forAsset, Asset domAsset, string holidayCalendar, bool supportsIntraday)
            : this()
        {
            When(new AssetPairRegistered(fordom, forAsset, domAsset, holidayCalendar, supportsIntraday));
        }

        /// <summary>
        /// Gets or sets the asset that is being quoted in the asset pair.
        /// This represents the "for" side of the pair (e.g., in a currency pair, USD in USD/EUR).
        /// </summary>
        public Asset ForAsset { get; set; }

        /// <summary>
        /// Gets or sets the asset that serves as the "domestic" asset in the asset pair.
        /// This represents the opposite side of the "quoted" asset (e.g., in a currency pair, EUR in USD/EUR).
        /// </summary>
        public Asset DomAsset { get; set; }

        /// <summary>
        /// Gets or sets the URL associated with the asset pair.
        /// This URL is typically used to retrieve quote or market information relevant to the pair.
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// Gets or sets the ticker symbol representing the asset pair,
        /// which uniquely identifies the combination of the "foreign" and "domestic" assets.
        /// </summary>
        public string Ticker { get; set; }

        /// <summary>
        /// Gets the set of dates for which quotes are available for the asset pair.
        /// This property contains unique instances of dates when specific trade-related
        /// quotes were added.
        /// </summary>
        public HashSet<Instant> QuoteDates { get; } = new();

        /// <summary>
        /// Gets a value indicating whether intraday trading or quoting is supported for this asset pair.
        /// Intraday support typically signifies that quotes and trades can occur within the same day,
        /// allowing for more granular and frequent updates to the asset pair's data.
        /// </summary>
        public bool SupportsIntraday { get; private set; }

        /// <summary>
        /// Gets or sets the identifier for the holiday calendar associated with the asset pair.
        /// This calendar defines the holidays or non-trading days that can affect trading or
        /// settlement operations related to the pair.
        /// </summary>
        public string HolidayCalendar { get; set; }

        /// <summary>
        /// Combines the asset identifiers of the given "for" asset and "dom" asset into a single string.
        /// </summary>
        /// <param name="forAsset">The asset representing the "for" currency or instrument.</param>
        /// <param name="domAsset">The asset representing the "dom" currency or instrument.</param>
        /// <returns>A string representing the concatenation of the two asset identifiers.</returns>
        public static string Fordom(Asset forAsset, Asset domAsset)
        {
            return forAsset.AssetId + domAsset.AssetId;
        }

        /// <summary>
        /// Combines the identifiers of the given "for" asset and "dom" asset into a single string.
        /// </summary>
        /// <param name="forAssetId">The identifier of the "for" asset.</param>
        /// <param name="domAssetId">The identifier of the "dom" asset.</param>
        /// <returns>A string representing the combined identifier of the two assets.</returns>
        public static string Fordom(string forAssetId, string domAssetId)
        {
            return forAssetId + domAssetId;
        }

        /// <summary>
        /// Adds a quote to the asset pair with specified date, close, open, low, and high values.
        /// </summary>
        /// <param name="date">The date of the quote being added.</param>
        /// <param name="close">The closing price of the quote.</param>
        /// <param name="open">The opening price of the quote.</param>
        /// <param name="low">The lowest price of the quote.</param>
        /// <param name="high">The highest price of the quote.</param>
        public void AddQuote(Instant date, double close, double open, double low, double high)
        {
            When(new QuoteAdded(date, close, open, low, high));
        }

        /// <summary>
        /// Associates a new ticker with the AssetPair.
        /// </summary>
        /// <param name="ticker">The ticker symbol to add to the AssetPair.</param>
        public void AddTicker(string ticker)
        {
            When(new QuoteTickerAdded(ticker));
        }

        /// <summary>
        /// Associates a URL with the <see cref="AssetPair"/> for quote information.
        /// </summary>
        /// <param name="url">The URL to be associated with the asset pair.</param>
        public void AddUrl (string url)
        {
            When(new QuoteUrlAdded(url));
        }

        /// <summary>
        /// Adjusts the asset pair to account for a stock split with the specified ratio.
        /// </summary>
        /// <param name="ratio">The ratio of the stock split, indicating the number of new shares issued for each existing share.</param>
        public void SplitStock(double ratio)
        {
            When(new StockSplitAdded(ForAsset, DomAsset, ratio));
        }

        private void ApplyEvent (AssetPairRegistered e)
        {
            Id = e.Fordom;
            ForAsset = e.ForAsset;
            DomAsset = e.DomAsset;
            SupportsIntraday = e.SupportsIntraday;
            HolidayCalendar = e.HolidayCalendar;
        }

        private void ApplyEvent (QuoteAdded e)
        {
            QuoteDates.Add(e.Date);
        }  

        private void ApplyEvent (QuoteUrlAdded e)
        {
            Url = e.Url;
        }

        private void ApplyEvent(QuoteTickerAdded e)
        {
            Ticker = e.Ticker;
        }
    }
}
