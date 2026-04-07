using System;
using System.Collections.Generic;
using System.Linq;
using ZES.Interfaces.Net;

namespace Chronos.Core.Net;

/// <summary>
/// Provides functionality for searching tickers and exchanges using the EODHD search API.
/// </summary>
public class WebSearchApi : IWebSearchApi
{
    private static string ApiKey => Environment.GetEnvironmentVariable("EQUITY_APIKEY");

    /// <inheritdoc/>
    public string GetCurrency(IJsonResult result)
    {
        var r = result as JsonResult;
        var data = r?.FirstOrDefault();
        return data != null ? $"{data.Currency}" : string.Empty;    
    }

    /// <summary>
    /// Constructs a URL for searching tickers by symbol.
    /// </summary>
    /// <param name="ticker">The ticker symbol to search for.</param>
    /// <returns>A formatted URL string for the ticker search API.</returns>
    public string GetUrl(string ticker)
    {
        return $"https://eodhd.com/api/search/{ticker}?api_token={ApiKey ?? string.Empty}&fmt=json";
    }

    /// <inheritdoc/>
    public IEnumerable<string> GetExchanges(IJsonResult result)
    {
        var r = result as JsonResult;
        return r?.Select(x => x.Exchange) ?? Enumerable.Empty<string>();
    }

    /// <inheritdoc/>
    public string GetTicker(IJsonResult result)
    {
        var r = result as JsonResult;
        var data = r?.FirstOrDefault();
        return data != null ? $"{data.Code}.{data.Exchange}" : string.Empty;
    }

    /// <inheritdoc/>
    public Type GetJsonResultType() => typeof(JsonResult);

    /// <summary>
    /// JSON result class for ticker search API responses.
    /// </summary>
    public class JsonResult : List<JsonResult.ExchangeData>, IWebSearchJsonResult
    {
        /// <inheritdoc/>
        public string RequestorId { get; set; }

        /// <summary>
        /// Represents data about an exchange listing for a ticker.
        /// </summary>
        public class ExchangeData
        {
            /// <summary>
            /// Gets or sets the ticker code/symbol.
            /// </summary>
            public string Code { get; set; }

            /// <summary>
            /// Gets or sets the asset type (e.g., "Common Stock", "ETF").
            /// </summary>
            public string Type { get; set; }

            /// <summary>
            /// Gets or sets the trading currency for this listing.
            /// </summary>
            public string Currency { get; set; }

            /// <summary>
            /// Gets or sets the exchange where this ticker is listed.
            /// </summary>
            public string Exchange { get; set; }
        }
    }
}
