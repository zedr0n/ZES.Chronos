using Chronos.Coins.Events;
using ZES.Infrastructure.Projections;
using ZES.Interfaces;
using ZES.Interfaces.Domain;
using ZES.Interfaces.EventStore;
using ZES.Interfaces.Pipes;

namespace Chronos.Coins.Queries
{
    public class StatsProjection : Projection<Stats>
    {
        public StatsProjection(IEventStore<IAggregate> eventStore, ILog log, IMessageQueue messageQueue, ITimeline timeline, Dispatcher.Builder streamDispatcher)
            : base(eventStore, log, messageQueue, timeline, streamDispatcher)
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