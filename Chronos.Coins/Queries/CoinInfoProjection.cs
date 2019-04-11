using System.Collections.Concurrent;
using Chronos.Coins.Events;
using ZES.Infrastructure.Projections;
using ZES.Interfaces;
using ZES.Interfaces.EventStore;
using ZES.Interfaces.Pipes;

namespace Chronos.Coins.Queries
{
    public class CoinInfoProjection : Projection<CoinInfoProjection.StateType> 
    {
        public class StateType
        {
            private readonly ConcurrentDictionary<string, CoinInfo> _coins = new ConcurrentDictionary<string, CoinInfo>();
            
            public CoinInfo Get(string id)
            {
                _coins.TryGetValue(id, out var coinInfo);
                return coinInfo;
            }

            public void Set(string id, CoinInfo c)
            {
                _coins[id] = c;
            } 
        }
        
        private static StateType When(CoinCreated e, StateType c)
        {
            c.Set(e.Name, new CoinInfo(e.Name, e.Ticker,e.Timestamp));
            return c;
        }

        public CoinInfoProjection(IEventStore<IAggregate> eventStore, ILog logger, IMessageQueue messageQueue, ITimeline timeline) : base(eventStore, logger, messageQueue, timeline)
        {
            Register<CoinCreated>(When);
        }
    }
}