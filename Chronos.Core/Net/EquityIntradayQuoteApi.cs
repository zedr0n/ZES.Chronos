namespace Chronos.Core.Net;

/// <summary>
/// Provides functionality for interacting with an equity intraday (real-time) data web API.
/// </summary>
public class EquityIntradayQuoteApi : EodhdIntradayQuoteApiBase
{
    /// <inheritdoc/>
    public override string GetSearchTicker(Asset forAsset, Asset domAsset) => $"{forAsset.AssetId}";

    /// <inheritdoc/>
    public override bool CanHandle(AssetType forAssetType, AssetType domAssetType, bool intraday) =>
        forAssetType == AssetType.Equity && domAssetType == AssetType.Currency && intraday;
}
