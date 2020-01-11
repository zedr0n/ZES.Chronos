using Chronos.Coins.Events;
using ZES.Infrastructure.Projections;
using ZES.Interfaces;
using ZES.Interfaces.Domain;
using ZES.Interfaces.EventStore;
using ZES.Interfaces.Pipes;

namespace Chronos.Coins.Queries
{
    public class StatsProjection : SingleProjection<Stats>
    {
        public StatsProjection(IEventStore<IAggregate> eventStore, ILog log, ITimeline timeline, IMessageQueue messageQueue) 
            : base(eventStore, log, timeline, messageQueue)
        {
            Register<CoinCreated>(When);
        }

        private static Stats When(IEvent e, Stats state)
        {
            lock (state)
            {
                state.NumberOfCoins++; 
            }

            return state;
        }
    }
}