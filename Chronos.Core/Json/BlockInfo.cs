using System.Collections.Generic;
using ZES.Interfaces.Net;

namespace Chronos.Core.Json
{
    /// <summary>
    /// V2 block info
    /// </summary>
    public class BlockInfoV2 : IJsonResult
    {
        /// <summary>
        /// Gets or sets the block height
        /// </summary>
        public int Height { get; set; }
        
        /// <summary>
        /// Gets or sets the miner address
        /// </summary>
        public string Miner { get; set; }
        
        /// <summary>
        /// Gets or sets the transactions
        /// </summary>
        public List<TxV2> Txs { get; set; }
        
        /// <summary>
        /// Gets or sets the uncles
        /// </summary>
        public List<UncleV2> Uncles { get; set; }
        
        /// <inheritdoc/>
        public string RequestorId { get; set; }
    }

    /// <summary>
    /// V2 tx info
    /// </summary>
    public class TxV2
    {
        /// <summary>
        /// Gets or sets the transaction hash
        /// </summary>
        public string TxHash { get; set; }
        
        /// <summary>
        /// Gets or sets the originating address
        /// </summary>
        public string From { get; set; }
        
        /// <summary>
        /// Gets or sets the target address
        /// </summary>
        public string To { get; set; }
        
        /// <summary>
        /// Gets or sets the tx amount
        /// </summary>
        public double Amount { get; set; }
        
        /// <summary>
        /// Gets or sets the transaction timestamp
        /// </summary>
        public long BlockTimeStamp { get; set; }
    }

    /// <summary>
    /// V2 uncle info
    /// </summary>
    public class UncleV2
    {
        /// <summary>
        /// Gets or sets the block height
        /// </summary>
        public int Height { get; set; }
        
        /// <summary>
        /// Gets or sets the uncle depth
        /// </summary>
        public int Depth { get; set; }
        
        /// <summary>
        /// Gets or sets the miner address
        /// </summary>
        public string Miner { get; set; }
        
        /// <summary>
        /// Gets or sets the hash of the uncle block
        /// </summary>
        public string UncleHash { get; set; }
        
        /// <summary>
        /// Gets or sets the uncle block timestamp
        /// </summary>
        public long UncleTimeStamp { get; set; }
    }
}