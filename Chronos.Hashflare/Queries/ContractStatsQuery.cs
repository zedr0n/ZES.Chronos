using ZES.Infrastructure.Domain;

namespace Chronos.Hashflare.Queries
{
    /// <inheritdoc />
    public class ContractStatsQuery : SingleQuery<ContractStats>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContractStatsQuery"/> class.
        /// </summary>
        public ContractStatsQuery()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContractStatsQuery"/> class.
        /// </summary>
        /// <param name="contractId">Contract identifier</param>
        public ContractStatsQuery(string contractId)
            : base(contractId)
        {
        }
    }
}