using Chronos.Hashflare.Events;
using ZES.Infrastructure.Projections;

namespace Chronos.Hashflare.Queries
{
    public class ContractsStatsCoinMinedHandler : ProjectionHandlerBase<ContractStats, CoinMinedByContract>
    {
        public override ContractStats Handle(CoinMinedByContract e, ContractStats state)
        {
            state.Mined += e.Quantity;
            return state;
        }
    }
}