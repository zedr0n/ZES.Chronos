using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using ZES.Infrastructure.Utils;
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
    public override string GetUrl(string ticker, IEnumerable<Time> dates, bool enforceCache = false)
    {
        var datesList = dates?.ToList();
        if (datesList == null || datesList.Count == 0)
            return null;
            
        var fromDate = datesList.Min().ToDateTime().ToString(DateFormat, new DateTimeFormatInfo());
        var toDate = datesList.Max().ToDateTime().ToString(DateFormat, new DateTimeFormatInfo());
        return $"{BaseUrl}/{Endpoint}/{ticker}?from={fromDate}&to={toDate}&fmt=json&api_token={ApiKey ?? string.Empty}";
    }

    /// <inheritdoc/>
    public override (Time Timestamp, double Value) GetValue(IJsonResult result)
    {
        var r = result as JsonResult;
        var data = r?.SingleOrDefault();
        if (data == null)
            return (null, double.NaN);
        
        var time = data.Date.ToTime() ?? Time.FromUnixTimeSeconds(data.Timestamp);
        return (time, data.Close);
    }

    /// <inheritdoc/>
    [SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1010:Opening square brackets should be spaced correctly", Justification = "?? [] bug")]
    public override IEnumerable<(Time Timestamp, double Value)> GetValues(IJsonResult result)
    {
        var r = result as JsonResult;
        return r?.Select(x => (x.Date != null ? DateTime.Parse(x.Date).ToTime() : Time.FromUnixTimeSeconds(x.Timestamp), x.Close)) ?? [];
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
            /// Gets or sets the date associated with the price data.
            /// </summary>
            public string Date { get; set; }

            /// <summary>
            /// Gets or sets the timestamp representing the specific point in time associated with the price data.
            /// </summary>
            public long Timestamp { get; set; }
            
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
