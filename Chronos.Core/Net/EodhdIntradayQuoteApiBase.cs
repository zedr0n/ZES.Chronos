using System;
using ZES.Interfaces.Clocks;
using ZES.Interfaces.Net;

namespace Chronos.Core.Net;

/// <summary>
/// Abstract base class for EODHD Intraday (Real-time) API implementations.
/// </summary>
public abstract class EodhdIntradayQuoteApiBase : EodhdQuoteApiBase
{
    private const string Endpoint = "real-time";

    /// <inheritdoc/>
    public override string GetUrl(string ticker, Time date = null, bool enforceCache = false)
    {
        return $"{BaseUrl}/{Endpoint}/{ticker}?&fmt=json&api_token={ApiKey ?? string.Empty}{(enforceCache ? string.Empty : ";nocache")}";
    }

    /// <inheritdoc/>
    public override double GetValue(IJsonResult result)
    {
        var r = result as JsonResult;
        return r?.Close ?? double.NaN;
    }
    
    /// <inheritdoc/>
    public override Type GetJsonResultType() => typeof(JsonResult);

    /// <summary>
    /// Represents the JSON result of an intraday (real-time) data query, containing current market data.
    /// </summary>
    public class JsonResult : IWebQuoteJsonResult
    {
        /// <summary>
        /// Gets or sets the identifier of the requestor associated with the API response
        /// or data request. Used to track or log the source of the data request.
        /// </summary>
        public string RequestorId { get; set; }

        /// <summary>
        /// Gets or sets the opening price of the asset for the current intraday period.
        /// </summary>
        public double Open { get; set; }

        /// <summary>
        /// Gets or sets the highest price reached by the asset during the current intraday period.
        /// </summary>
        public double High { get; set; }

        /// <summary>
        /// Gets or sets the lowest price reached by the asset during the current intraday period.
        /// </summary>
        public double Low { get; set; }

        /// <summary>
        /// Gets or sets the current (most recent) price of the asset.
        /// </summary>
        /// <remarks>
        /// For intraday data, this represents the latest traded price of the asset,
        /// updated in real-time or near real-time.
        /// </remarks>
        public double Close { get; set; } = double.NaN;

        /// <summary>
        /// Gets or sets the closing price from the previous trading session.
        /// </summary>
        /// <remarks>
        /// This property records the official closing price from the last completed trading session,
        /// used as a baseline for calculating intraday price changes.
        /// </remarks>
        public double PreviousClose { get; set; }

        /// <summary>
        /// Gets or sets the intraday price change relative to the previous close.
        /// </summary>
        /// <remarks>
        /// This property represents the difference between the current price and the previous session's closing price,
        /// calculated as: Current Price - Previous Close.
        /// </remarks>
        public double Change { get; set; }

        /// <summary>
        /// Gets or sets the Unix timestamp (in seconds) when this intraday data was captured.
        /// </summary>
        /// <remarks>
        /// Represents the exact moment when the current price data was recorded,
        /// stored as seconds elapsed since January 1, 1970 (Unix epoch).
        /// </remarks>
        public long Timestamp { get; set; }
    }
}
