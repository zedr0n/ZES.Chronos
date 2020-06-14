using Chronos.Hashflare.Events;
using ZES.Infrastructure.Projections;

namespace Chronos.Hashflare.Queries
{
    /// <inheritdoc />
    public class ContractStatsContractCreatedHandler : ProjectionHandlerBase<ContractStats, ContractCreated>
    {
        /// <inheritdoc />
        public override ContractStats Handle(ContractCreated e, ContractStats state)
        {
            state.Type = e.Type;
            state.Mined = 0;
            state.ContractId = e.ContractId;
            return state;
        }
    }
}