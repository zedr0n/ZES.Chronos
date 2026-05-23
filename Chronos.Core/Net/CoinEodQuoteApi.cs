using System;
using System.Collections.Generic;
using System.Linq;
using ZES.Interfaces.Clocks;

namespace Chronos.Core.Net;

/// <summary>
/// Provides functionality for interacting with a cryptocurrency data web API.
/// </summary>
public class CoinEodQuoteApi : EodhdEodQuoteApiBase
{
    private const string Endpoint = "intraday";
    
    /// <inheritdoc/>
    public override string GetSearchTicker(Asset forAsset, Asset domAsset) => $"{forAsset.AssetId}-{domAsset.AssetId}";

    /// <inheritdoc/>
    public override string GetPreciseUrl(string ticker, Time date = null, bool enforceCache = false)
    {
        if (date == null)
            return null; 
        
        var fromTimestamp = date.ToUnixTimeSeconds();
        var toTimestamp = fromTimestamp + 59;
        return $"{BaseUrl}/{Endpoint}/{ticker}?from={fromTimestamp}&to={toTimestamp}&interval=1m&fmt=json&api_token={ApiKey ?? string.Empty}";
    }

    /// <inheritdoc/>
    public override string GetPreciseUrl(string ticker, List<Time> dates, bool enforceCache = false)
    {
        if (dates.Count == 1)
           return GetPreciseUrl(ticker, dates.FirstOrDefault(), enforceCache); 
        
        var fromTimestamp = dates.Min().ToUnixTimeSeconds() - 59;
        var toTimestamp = dates.Max().ToUnixTimeSeconds() + 59;
        return $"{BaseUrl}/{Endpoint}/{ticker}?from={fromTimestamp}&to={toTimestamp}&interval=1m&fmt=json&api_token={ApiKey ?? string.Empty}";
    }

    /// <inheritdoc/>
    public override bool CanHandle(AssetType forAssetType, AssetType domAssetType, bool intraday) =>
        forAssetType == AssetType.Coin && domAssetType == AssetType.Currency && !intraday;
}
