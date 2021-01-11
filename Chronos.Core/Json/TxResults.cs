using ZES.Infrastructure;
using ZES.Interfaces.Net;

namespace Chronos.Core.Json
{
    /// <summary>
    /// TX Results JSON
    /// </summary>
    public class TxResults : JsonList<Tx>, IJsonResult
    { }

    /// <summary>
    /// Transaction JSON
    /// </summary>
    public class Tx
    {
        /// <summary>
        /// Gets or sets the transaction hash
        /// </summary>
        public string Hash { get; set; }
        
        /// <summary>
        /// Gets or sets the originating address
        /// </summary>
        public string From { get; set; }
        
        /// <summary>
        /// Gets or sets the target address
        /// </summary>
        public string To { get; set; }
        
        /// <summary>
        /// Gets or sets the transaction amount
        /// </summary>
        public double Amount { get; set; }
        
        /// <summary>
        /// Gets or sets the transaction timestamp
        /// </summary>
        public long ReceiveTime { get; set; }
    }
}