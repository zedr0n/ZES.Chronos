using ZES.Infrastructure.Domain;

namespace Chronos.Hashflare.Events
{
    /// <inheritdoc />
    public class CoinMinedByContract : Event
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CoinMinedByContract"/> class.
        /// </summary>
        /// <param name="contractId">Contract identifier</param>
        /// <param name="type">Coin type</param>
        /// <param name="quantity">Amount mined</param>
        public CoinMinedByContract(string contractId, string type, double quantity)
        {
            ContractId = contractId;
            Type = type;
            Quantity = quantity;
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
        /// Gets coin quantity 
        /// </summary>
        public double Quantity { get; }
    }
}