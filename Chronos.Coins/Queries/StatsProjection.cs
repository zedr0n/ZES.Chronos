using Chronos.Coins.Events;
using ZES.Infrastructure.Projections;
using ZES.Interfaces;
using ZES.Interfaces.EventStore;
using ZES.Interfaces.Pipes;

namespace Chronos.Coins.Queries
{
    public class StatsProjection : Projection<ValueState<Stats>>
    {
        public static ValueState<Stats> When(IEvent e, ValueState<Stats> state)
        {
            lock (state)
            {
                state.Value.NumberOfCoins++; 
            }

            return state;
        }
        
        public StatsProjection(IEventStore<IAggregate> eventStore, ILog logger, IMessageQueue messageQueue, ITimeline timeline) : base(eventStore, logger, messageQueue, timeline)
        {
            Register<CoinCreated>(When);
        }
    }
}