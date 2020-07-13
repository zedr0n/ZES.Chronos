using ZES.Infrastructure.Domain;

namespace Chronos.Hashflare.Events
{
    /// <inheritdoc />
    public class ContractCreated : Event
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContractCreated"/> class.
        /// </summary>
        /// <param name="contractId">Contract identifier</param>
        /// <param name="type">Hash type</param>
        /// <param name="quantity">Hash amount</param>
        /// <param name="total">Total cost (in USD)</param>
        public ContractCreated(string contractId, string type, int quantity, int total)
        {
            Type = type;
            Quantity = quantity;
            Total = total;
            ContractId = contractId;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContractCreated"/> class.
        /// </summary>
        public ContractCreated()
        {
        }

        /// <summary>
        /// Gets contract identifier
        /// </summary>
        public string ContractId { get; internal set; }
        
        /// <summary>
        /// Gets coin type
        /// </summary>
        public string Type { get; internal set; }
        
        /// <summary>
        /// Gets hash rate quantity 
        /// </summary>
        public int Quantity { get; internal set; }
        
        /// <summary>
        /// Gets total contract cost ( in USD ) 
        /// </summary>
        public int Total { get; internal set; }
    }
}