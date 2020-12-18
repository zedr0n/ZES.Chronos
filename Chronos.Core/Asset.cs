using System;
using System.Collections.Generic;
using ZES.Infrastructure;

#pragma warning disable 660
#pragma warning disable 661

namespace Chronos.Core
{
    /// <summary>
    /// Asset value object
    /// </summary>
    public class Asset : ValueObject, IEquatable<Asset>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Asset"/> class.
        /// </summary>
        /// <param name="assetId">Asset identifier</param>
        /// <param name="ticker">Asset ticker</param>
        /// <param name="assetType">Asset type</param>
        public Asset(string assetId, string ticker, Type assetType)
        {
            AssetId = assetId;
            AssetType = assetType;
            Ticker = ticker;
        }
            
        /// <summary>
        /// Asset types
        /// </summary>
        public enum Type
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
        /// Gets asset identifier
        /// </summary>
        public string AssetId { get; private set; }
        
        /// <summary>
        /// Gets asset ticker
        /// </summary>
        public string Ticker { get; private set; }

        /// <summary>
        /// Gets asset type
        /// </summary>
        public Type AssetType { get; private set; }

        /// <summary>
        /// Equal operator
        /// </summary>
        /// <param name="left">Left instance</param>
        /// <param name="right">Right instance</param>
        /// <returns>True if equal</returns>
        public static bool operator ==(Asset left, Asset right)
        {
            return EqualOperator(left, right);
        }

        /// <summary>
        /// Not equal operator
        /// </summary>
        /// <param name="left">Left instance</param>
        /// <param name="right">Right instance</param>
        /// <returns>True if not equal</returns>
        public static bool operator !=(Asset left, Asset right)
        {
            return NotEqualOperator(left, right);
        }

        /// <inheritdoc />
        public bool Equals(Asset other)
        {
            return base.Equals(other);
        }

        /// <inheritdoc />
        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return AssetId;
            yield return AssetType;
        }
    }
}