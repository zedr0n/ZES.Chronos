using System.Collections.Generic;
using ZES.Interfaces.Net;

namespace Chronos.Core.Json
{
    /// <summary>
    /// Wallet info JSON
    /// </summary>
    public class AddressInfo : IJsonResult
    {
        /// <summary>
        /// Gets or sets the wallet address
        /// </summary>
        public string Hash { get; set; }
        
        /// <summary>
        /// Gets or sets the final wallet balance
        /// </summary>
        public string Balance { get; set; }
        
        /// <summary>
        /// Gets or sets the list of transactions
        /// </summary>
        public List<Tx> Txs { get; set; }
        
        /// <summary>
        /// Gets or sets the list of mined blocks
        /// </summary>
        public List<MinedBlock> MinedBlocks { get; set; }

        /// <inheritdoc/>
        public string RequestorId { get; set; }
    }
}