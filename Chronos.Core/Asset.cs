#nullable enable
using System;
using System.Collections.Generic;

#pragma warning disable 660
#pragma warning disable 661

namespace Chronos.Core
{
    /// <summary>
    /// Asset types
    /// </summary>
    public enum AssetType
    {
        /// <summary>
        /// Crypto coin
        /// </summary>
        Coin,
                
        /// <summary>
        /// Regular equity
        /// </summary>
        Equity,
            
        /// <summary>
        /// Currency
        /// </summary>
        Currency,
    }

    /// <summary>
    /// Asset value object
    /// </summary>
    /// <param name="AssetId">Asset identifier</param>
    /// <param name="AssetType">Asset type</param>
    public record Asset(string? AssetId, AssetType AssetType)
    {
        /// <summary>
        /// Validates whether the current Asset instance has a non-null and non-empty AssetId.
        /// </summary>
        /// <returns>True if the AssetId is valid; otherwise, false.</returns>
        public bool IsValid() => AssetId != null && AssetId.Length > 0;
        
        /// <summary>
        /// Converts any Asset (including derived types such as Currency) into its base Asset instance representation
        /// </summary>
        /// <returns>The base Asset instance representing the current object</returns>
        public Asset ToAsset() => GetType() == typeof(Asset) ? this : new Asset(AssetId, AssetType);

        /// <summary>
        /// Equality comparer that treats Asset and Currency (and other derived types) as equivalent based on AssetId and AssetType
        /// </summary>
        public sealed class Comparer : IEqualityComparer<Asset>
        {
            /// <summary>
            /// Singleton instance
            /// </summary>
            public static readonly Comparer Instance = new();

            /// <inheritdoc/>
            public bool Equals(Asset? x, Asset? y)
            {
                if (ReferenceEquals(x, y))
                    return true;
                if (x is null || y is null)
                    return false;
                return x.AssetId == y.AssetId && x.AssetType == y.AssetType;
            }

            /// <inheritdoc/>
            public int GetHashCode(Asset obj)
            {
                return HashCode.Combine(obj.AssetId ?? string.Empty, obj.AssetType);
            }
        }
    }
}