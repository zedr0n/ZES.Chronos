using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Hashflare.Commands
{
    /// <summary>
    /// Create the contract
    /// </summary>
    public class CreateContract : Command, ICreateCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateContract"/> class.
        /// </summary>
        public CreateContract() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateContract"/> class.
        /// </summary>
        /// <param name="contractId">Contract identifier</param>
        /// <param name="type">Hash type</param>
        /// <param name="quantity">Hash quantity</param>
        /// <param name="total">Total cost ( in USD )</param>
        public CreateContract(string contractId, string type, int quantity, int total) 
            : base(contractId) 
        {
            ContractId = contractId;
            Type = type;
            Quantity = quantity;
            Total = total; 
        }

        /// <summary>
        /// Gets contract identifier
        /// </summary>
        public string ContractId { get; }
        
        /// <summary>
        /// Gets contract coin type
        /// </summary>
        public string Type { get; }
        
        /// <summary>
        /// Gets contract hash rate amount 
        /// </summary>
        public int Quantity { get; }
        
        /// <summary>
        /// Gets contract cost ( in USD ) 
        /// </summary>
        public int Total { get; }
    }
}
