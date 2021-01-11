using ZES.Infrastructure;

namespace Chronos.Core.Json
{
    /// <summary>
    /// Mined blocks JSON result
    /// </summary>
    public class MinedResults : JsonList<MinedBlock>
    {
    }
    
    /// <summary>
    /// Mined block JSON
    /// </summary>
    public class MinedBlock
    {
        /// <summary>
        /// Gets or sets the block hash
        /// </summary>
        public string Blockhash { get; set; }
        
        /// <summary>
        /// Gets or sets the block timestamp
        /// </summary>
        public long Timestamp { get; set; }
        
        /// <summary>
        /// Gets or sets the miner
        /// </summary>
        public string Miner { get; set; }
        
        /// <summary>
        /// Gets or sets the block reward
        /// </summary>
        public double Feereward { get; set; }
    }
}