using System;
using System.Collections.Concurrent;
using Coins.Events;
using ZES.Infrastructure;
using ZES.Infrastructure.Projections;
using ZES.Interfaces;
using ZES.Interfaces.EventStore;
using ZES.Interfaces.Pipes;

namespace Coins.Projections
{
    //[Reset]
    /// <summary>
    /// Coin info read model
    /// </summary>
    public class CoinInfoProjection : Projection 
    {
        private readonly ConcurrentDictionary<string, string> _tickers = new ConcurrentDictionary<string, string>();

        public CoinInfo Get(string name)
        {
            return new CoinInfo
            {
                Name = name,
                Ticker = _tickers[name]
            };
        }
        private void When(CoinCreated e)
        {
            _tickers.TryAdd(e.Name, e.Ticker);
        }

        protected CoinInfoProjection(IEventStore<IAggregate> eventStore, ILog logger, IMessageQueue messageQueue) : base(eventStore, logger, messageQueue)
        {
            Register<CoinCreated>(When);
        }
    }
}