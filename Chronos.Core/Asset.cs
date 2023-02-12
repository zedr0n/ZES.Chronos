using System;
using System.Collections.Generic;
using ZES.Infrastructure;

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
    /// <param name="Ticker">Asset ticker</param>
    /// <param name="AssetType">Asset type</param>
    public record Asset(string AssetId, string Ticker, AssetType AssetType);
}