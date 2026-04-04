using System.Collections.Generic;
using ZES.Interfaces.Net;

namespace Chronos.Core.Net;

/// <summary>
/// Provides functionality for interacting with an FX data web API.
/// </summary>
public class FxEodQuoteApi : EodhdEodQuoteApiBase
{
    /// <inheritdoc/>
    public override string GetSearchTicker(Asset forAsset, Asset domAsset) => $"{forAsset.AssetId}{domAsset.AssetId}";

    /// <inheritdoc/>
    public override bool CanHandle(AssetType forAssetType, AssetType domAssetType, bool intraday) =>
        forAssetType == AssetType.Currency && domAssetType == AssetType.Currency && !intraday;
}
