using System;
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
    public abstract double GetValue(IJsonResult result);

    /// <inheritdoc/>
    public abstract string GetSearchTicker(Asset forAsset, Asset domAsset);

    /// <inheritdoc/>
    public abstract Type GetJsonResultType();

    /// <inheritdoc/>
    public abstract bool CanHandle(AssetType forAssetType, AssetType domAssetType, bool intraday);
}
