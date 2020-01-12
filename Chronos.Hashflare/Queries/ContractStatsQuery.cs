using ZES.Infrastructure.Domain;

namespace Chronos.Hashflare.Queries
{
    public class ContractStatsQuery : Query<ContractStats>
    { 
        public ContractStatsQuery() { }

        public ContractStatsQuery(string txId)
        {
            TxId = txId;
        }
        
        public string TxId { get; }
    }
}