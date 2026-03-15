using System;
using ZES.Interfaces.GraphQL;

namespace Chronos.Core
{
    /// <summary>
    /// Currency asset
    /// </summary>
    public sealed record Currency(string AssetId) : Asset(AssetId, AssetId, AssetType.Currency), IGraphQlInputType
    {
        /// <inheritdoc/>
        protected override Type EqualityContract => typeof(Asset);
    }
}