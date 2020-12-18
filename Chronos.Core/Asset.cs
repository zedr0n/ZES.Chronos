using System.Collections.Generic;
using ZES.Infrastructure;

namespace Chronos.Core
{
    /// <summary>
    /// Asset value object
    /// </summary>
    public class Asset : ValueObject
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

        /// <inheritdoc />
        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return AssetId;
            yield return AssetType;
        }
    }
}