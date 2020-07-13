using Chronos.Hashflare.Events;
using ZES.Interfaces;
using ZES.Interfaces.Domain;

namespace Chronos.Hashflare.Queries
{
    /// <summary>
    /// Projection handler for HashflareStats
    /// </summary>
    public class HashflareStatsHandler : IProjectionHandler<HashflareStats, ContractCreated>, IProjectionHandler<HashflareStats, ContractExpired>, IProjectionHandler<HashflareStats, HashflareRegistered>
    {
        /// <inheritdoc />
        public HashflareStats Handle(IEvent e, HashflareStats state) => Handle((dynamic)e, state);

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        public HashflareStats Handle(HashflareRegistered e, HashflareStats state)
        {
            state.Username = e.Username;
            return state;
        }
    }
}