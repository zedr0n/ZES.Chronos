using ZES.Infrastructure.Domain;

namespace Chronos.Hashflare.Events
{
    /// <inheritdoc />
    public class ContractExpired : Event
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContractExpired"/> class.
        /// </summary>
        /// <param name="contractId">Contract identifier</param>
        public ContractExpired(string contractId)
        {
            ContractId = contractId;
        }
        
        /// <summary>
        /// Gets contract identifier
        /// </summary>
        public string ContractId { get; }
    }
}