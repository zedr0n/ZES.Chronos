using Chronos.Hashflare.Events;
using ZES.Infrastructure.Projections;

namespace Chronos.Hashflare.Queries
{
    /// <inheritdoc />
    public class ContractsStatsCoinMinedHandler : ProjectionHandlerBase<ContractStats, CoinMinedByContract>
    {
        /// <inheritdoc />
        public override ContractStats Handle(CoinMinedByContract e, ContractStats state)
        {
            state.Mined += e.Quantity;
            return state;
        }
    }
}