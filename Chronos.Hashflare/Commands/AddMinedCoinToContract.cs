using ZES.Infrastructure.Domain;

namespace Chronos.Hashflare.Commands
{
    /// <inheritdoc />
    public class AddMinedCoinToContract : Command
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AddMinedCoinToContract"/> class.
        /// </summary>
        public AddMinedCoinToContract() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="AddMinedCoinToContract"/> class.
        /// </summary>
        /// <param name="contractId">Contract id</param>
        /// <param name="type">Coin type</param>
        /// <param name="quantity">Mined amount</param>
        public AddMinedCoinToContract(string contractId, string type, double quantity) 
            : base(contractId) 
        {
            ContractId = contractId;
            Type = type;
            Quantity = quantity;
        }

        /// <summary>
        /// Gets or sets contract identifier
        /// </summary>
        public string ContractId { get; set; }
        
        /// <summary>
        /// Gets or sets coin type 
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// Gets or sets coin amount mined
        /// </summary>
        public double Quantity { get; set; }
    }
}
