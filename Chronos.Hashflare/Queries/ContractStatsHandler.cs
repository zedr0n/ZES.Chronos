using Chronos.Hashflare.Events;
using ZES.Interfaces;
using ZES.Interfaces.Domain;

namespace Chronos.Hashflare.Queries
{
    /// <summary>
    /// Projection handler for ContractStats
    /// </summary>
    public class ContractStatsHandler : IProjectionHandler<ContractStats, ContractCreated>, IProjectionHandler<ContractStats, CoinMinedByContract>
    {
        /// <inheritdoc />
        public ContractStats Handle(IEvent e, ContractStats state) => Handle((dynamic)e, state);

        /// <inheritdoc />
        public ContractStats Handle(ContractCreated e, ContractStats state)
        {
            state.Type = e.Type;
            state.Mined = 0;
            state.ContractId = e.ContractId;
            return state;
        }

        /// <inheritdoc />
        public ContractStats Handle(CoinMinedByContract e, ContractStats state)
        {
            state.Mined += e.Quantity;
            return state;
        }
    }
}