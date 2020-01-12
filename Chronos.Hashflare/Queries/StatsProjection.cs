using Chronos.Hashflare.Events;
using ZES.Infrastructure.Projections;
using ZES.Interfaces;
using ZES.Interfaces.Domain;
using ZES.Interfaces.EventStore;
using ZES.Interfaces.Pipes;

namespace Chronos.Hashflare.Queries
{
    public class StatsProjection : SingleProjection<HashflareStats>
    {
        public StatsProjection(IEventStore<IAggregate> eventStore, ILog log, ITimeline timeline, IMessageQueue messageQueue) 
            : base(eventStore, log, timeline, messageQueue)
        {
            Register<HashrateBought>(When);
            Register<ContractExpired>(When);
        }

        private static HashflareStats When(HashrateBought e, HashflareStats state)
        {
            lock (state)
            {
                if (e.Type == "SHA-256")
                    state.BitcoinHashRate += e.Quantity;
            }
            
            return state;
        }

        private static HashflareStats When(ContractExpired e, HashflareStats state)
        {
            lock (state)
            {
                if (e.Type == "SHA-256")
                    state.BitcoinHashRate -= e.Quantity;
            }

            return state;
        }
    }
}