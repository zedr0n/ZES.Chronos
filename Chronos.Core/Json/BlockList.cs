using ZES.Infrastructure;

namespace Chronos.Core.Json
{
    /// <summary>
    /// Block list JSON
    /// </summary>
    public class BlockListV2 : JsonList<BlockV2>
    {
    }

    /// <summary>
    /// Block JSON
    /// </summary>
    public class BlockV2
    {
        /// <summary>
        /// Gets or sets the block height
        /// </summary>
        public int Height { get; set; }
        
        /// <summary>
        /// Gets or sets a value indicating whether block is in the main chain
        /// </summary>
        public bool IsMain { get; set; }
        
        /// <summary>
        /// Gets or sets the block hashh
        /// </summary>
        public string Blockhash { get; set; }
        
        /// <summary>
        /// Gets or sets the number of transactions in the block
        /// </summary>
        public int TxCount { get; set; }
        
        /// <summary>
        /// Gets or sets the block timestamp
        /// </summary>
        public long Blocktimestamp { get; set; }
        
        /// <summary>
        /// Gets or sets the block miner
        /// </summary>
        public string Miner { get; set; }
        
        /// <summary>
        /// Gets or sets the number of uncles
        /// </summary>
        public int UncleCount { get; set; }
    }
}