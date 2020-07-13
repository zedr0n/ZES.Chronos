using System.Collections.Concurrent;
using ZES.Interfaces.Domain;

namespace Chronos.Hashflare.Queries
{
    /// <summary>
    /// Hashflare statistics
    /// </summary>
    public class HashflareStats : IState
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HashflareStats"/> class.
        /// </summary>
        public HashflareStats() { }
        
        /// <summary>
        /// Gets contract details
        /// </summary>
        public ConcurrentDictionary<string, ContractDetails> Details { get; } = new ConcurrentDictionary<string, ContractDetails>();
        
        /// <summary>
        /// Gets or sets the account username 
        /// </summary>
        public string Username { get; set; }
        
        /// <summary>
        /// Gets or sets total SHA-256 hash rate
        /// </summary>
        public int BitcoinHashRate { get; set; }     
        
        /// <summary>
        /// Gets or sets total scrypt hash rate
        /// </summary>
        public int ScryptHashRate { get; set; }
        
        /// <summary>
        /// Contract details class
        /// </summary>
        public class ContractDetails
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="ContractDetails"/> class.
            /// </summary>
            /// <param name="type">Hash type</param>
            /// <param name="quantity">Hash quantity</param>
            public ContractDetails(string type, int quantity)
            {
                Type = type;
                Quantity = quantity;
            }
            
            /// <summary>
            /// Gets hash type
            /// </summary>
            public string Type { get; }
            
            /// <summary>
            /// Gets hash quantity
            /// </summary>
            public int Quantity { get; }
        }
    }
}