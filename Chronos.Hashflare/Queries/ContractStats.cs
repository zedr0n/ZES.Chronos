namespace Chronos.Hashflare.Queries
{
    /// <summary>
    /// Contract statistics
    /// </summary>
    public class ContractStats
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContractStats"/> class.
        /// </summary>
        /// <param name="contractId">Contract id</param>
        /// <param name="type">Coin type</param>
        /// <param name="mined">Amount mined</param>
        public ContractStats(string contractId, string type = "SHA-256", double mined = 0)
        {
            ContractId = contractId;
            Type = type;
            Mined = mined;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContractStats"/> class.
        /// </summary>
        public ContractStats()
        {
        }

        /// <summary>
        /// Gets contract identifier 
        /// </summary>
        public string ContractId { get; }
        
        /// <summary>
        /// Gets coin type
        /// </summary>
        public string Type { get; }
        
        /// <summary>
        /// Gets total amount mined
        /// </summary>
        public double Mined { get; }
    }
}