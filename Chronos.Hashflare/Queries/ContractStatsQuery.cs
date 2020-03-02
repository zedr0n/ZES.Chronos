using ZES.Infrastructure.Domain;

namespace Chronos.Hashflare.Queries
{
    /// <inheritdoc />
    public class ContractStatsQuery : Query<ContractStats>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContractStatsQuery"/> class.
        /// </summary>
        public ContractStatsQuery() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContractStatsQuery"/> class.
        /// </summary>
        /// <param name="contractId">Contract identifier</param>
        public ContractStatsQuery(string contractId)
        {
            ContractId = contractId;
        }
        
        /// <summary>
        /// Gets contract identifier
        /// </summary>
        public string ContractId { get; }
    }
}