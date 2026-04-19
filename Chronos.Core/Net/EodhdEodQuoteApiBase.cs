using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using ZES.Interfaces.Clocks;
using ZES.Interfaces.Net;

namespace Chronos.Core.Net;

/// <summary>
/// Abstract base class for EODHD EOD (End-of-Day) API implementations.
/// </summary>
public abstract class EodhdEodQuoteApiBase : EodhdQuoteApiBase
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string Endpoint = "eod";

    /// <inheritdoc/>
    public override string GetUrl(string ticker, Time date = null, bool enforceCache = false)
    {
        var dateString = date?.ToDateTime().ToString(DateFormat, new DateTimeFormatInfo());
        return $"{BaseUrl}/{Endpoint}/{ticker}?from={dateString}&to={dateString}&fmt=json&api_token={ApiKey ?? string.Empty}";
    }

    /// <inheritdoc/>
    public override double GetValue(IJsonResult result)
    {
        var r = result as JsonResult;
        var data = r?.SingleOrDefault();
        return data?.Close ?? double.NaN;
    }

    /// <inheritdoc/>
    public override Type GetJsonResultType() => typeof(JsonResult);

    /// <summary>
    /// Standard JSON result class for EODHD API responses.
    /// </summary>
    public class JsonResult : List<JsonResult.PriceData>, IWebQuoteJsonResult
    {
        /// <inheritdoc/>
        public string RequestorId { get; set; }

        /// <summary>
        /// Represents price data, including open, high, low, and close values for a given asset.
        /// </summary>
        public class PriceData
        {
            /// <summary>
            /// Gets or sets the opening price for the asset on a specific trading day.
            /// </summary>
            public double Open { get; set; }

            /// <summary>
            /// Gets or sets the high price value for the asset during a specific time period.
            /// </summary>
            public double High { get; set; }

            /// <summary>
            /// Gets or sets the lowest price value for the asset in the specified time period.
            /// </summary>
            public double Low { get; set; }

            /// <summary>
            /// Gets or sets the close price of an asset.
            /// </summary>
            public double Close { get; set; } = double.NaN;
        }
    }
}
