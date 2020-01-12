using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Hashflare.Queries
{
    public class ContractStatsQueryHandler : QueryHandlerBaseEx<ContractStatsQuery, ContractStats, ContractStatsProjection.Results>
    {
        public ContractStatsQueryHandler(IProjection<ContractStatsProjection.Results> projection) 
            : base(projection)
        {
        }

        protected override ContractStats Handle(IProjection<ContractStatsProjection.Results> projection, ContractStatsQuery query)
         => new ContractStats(query.TxId, projection.State.Ratio(query.TxId));
    }
}