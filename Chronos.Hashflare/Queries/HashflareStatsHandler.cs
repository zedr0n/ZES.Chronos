using Chronos.Hashflare.Events;
using ZES.Interfaces;
using ZES.Interfaces.Domain;

namespace Chronos.Hashflare.Queries
{
    public class HashflareStatsHandler : IProjectionHandler<HashflareStats, ContractCreated>, IProjectionHandler<HashflareStats, ContractExpired>
    {
        public HashflareStats Handle(IEvent e, HashflareStats state) => Handle((dynamic) e, state);

        public HashflareStats Handle(ContractCreated e, HashflareStats state)
        {
            lock (state)
            {
                state.Details[e.ContractId] = new HashflareStats.ContractDetails(e.Type, e.Quantity);
                
                if (e.Type == "SHA-256")
                    state.BitcoinHashRate += e.Quantity;
                else
                    state.ScryptHashRate += e.Quantity;
            }
            
            return state;
        }

        public HashflareStats Handle(ContractExpired e, HashflareStats state)
        {
            lock (state)
            {
                if (!state.Details.ContainsKey(e.ContractId))
                    return state;
                
                var details = state.Details[e.ContractId];
                
                if (details.Type == "SHA-256")
                    state.BitcoinHashRate -= details.Quantity;
                else
                    state.ScryptHashRate -= details.Quantity;
            }

            return state;
        }
    }
}