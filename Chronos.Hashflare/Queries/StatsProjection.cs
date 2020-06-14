using Chronos.Hashflare.Events;
using ZES.Infrastructure.Projections;
using ZES.Interfaces;
using ZES.Interfaces.Domain;
using ZES.Interfaces.EventStore;
using ZES.Interfaces.Pipes;

namespace Chronos.Hashflare.Queries
{
    /// <inheritdoc />
    public class StatsProjection : GlobalProjection<HashflareStats>
    {
        /// <inheritdoc />
        public StatsProjection(IEventStore<IAggregate> eventStore, ILog log, ITimeline timeline, IMessageQueue messageQueue) 
            : base(eventStore, log, timeline, messageQueue)
        {
            Register<ContractCreated>(When);
            Register<ContractExpired>(When);
        }

        private static HashflareStats When(ContractCreated e, HashflareStats state)
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

        private static HashflareStats When(ContractExpired e, HashflareStats state)
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