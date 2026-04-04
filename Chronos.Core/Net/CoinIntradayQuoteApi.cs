namespace Chronos.Core.Net;

/// <summary>
/// Provides functionality for interacting with a cryptocurrency intraday (real-time) data web API.
/// </summary>
public class CoinIntradayQuoteApi : EodhdIntradayQuoteApiBase
{
    /// <inheritdoc/>
    public override string GetSearchTicker(Asset forAsset, Asset domAsset) => $"{forAsset.AssetId}-{domAsset.AssetId}";

    /// <inheritdoc/>
    public override bool CanHandle(AssetType forAssetType, AssetType domAssetType, bool intraday) =>
        forAssetType == AssetType.Coin && domAssetType == AssetType.Currency && intraday;
}
