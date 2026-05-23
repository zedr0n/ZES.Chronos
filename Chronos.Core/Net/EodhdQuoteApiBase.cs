using System;
using System.Collections.Generic;
using System.Linq;
using ZES.Interfaces.Clocks;
using ZES.Interfaces.Net;

namespace Chronos.Core.Net;

/// <summary>
/// Core abstract base class for all EODHD API implementations.
/// </summary>
public abstract class EodhdQuoteApiBase : IWebQuoteApi
{
    /// <summary>
    /// Base URL for EODHD API
    /// </summary>
    protected const string BaseUrl = "https://eodhd.com/api";

    /// <summary>
    /// Gets API key from environment variable
    /// </summary>
    protected static string ApiKey => Environment.GetEnvironmentVariable("EQUITY_APIKEY");

    /// <inheritdoc/>
    public abstract string GetUrl(string ticker, Time date = null, bool enforceCache = false);

    /// <inheritdoc/>
    public virtual string GetUrl(string ticker, IEnumerable<Time> dates, bool enforceCache = false)
     => GetUrl(ticker, dates.FirstOrDefault(), enforceCache);

    /// <inheritdoc/>
    public virtual string GetPreciseUrl(string ticker, Time date = null, bool enforceCache = false) => GetUrl(ticker, date, enforceCache);

    /// <inheritdoc/>
    public virtual string GetPreciseUrl(string ticker, List<Time> dates, bool enforceCache = false) 
     => GetUrl(ticker, dates, enforceCache);

    /// <inheritdoc/>
    public abstract (Time Timestamp, double Value) GetValue(IJsonResult result);

    /// <inheritdoc/>
    public virtual IEnumerable<(Time Timestamp, double Value)> GetValues(IJsonResult result)
        => new List<(Time Timestamp, double Value)> { GetValue(result) };

    /// <inheritdoc/>
    public abstract string GetSearchTicker(Asset forAsset, Asset domAsset);

    /// <inheritdoc/>
    public abstract Type GetJsonResultType();

    /// <inheritdoc/>
    public abstract bool CanHandle(AssetType forAssetType, AssetType domAssetType, bool intraday);

    /// <inheritdoc/>
    public virtual bool SupportsMultipleTimestamps() => true;
}
