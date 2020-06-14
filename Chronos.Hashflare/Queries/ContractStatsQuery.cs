using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Hashflare.Queries
{
    /// <inheritdoc />
    public class ContractStatsQuery : Query<ContractStats>, ISingleQuery<ContractStats>
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
            Id = contractId;
        }

        /// <inheritdoc />
        public string Id { get; }
    }
}