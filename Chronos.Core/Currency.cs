namespace Chronos.Core
{
    /// <summary>
    /// Currency asset
    /// </summary>
    public sealed record Currency(string AssetId) : Asset(AssetId, AssetId, AssetType.Currency);
}