using System.Collections.Concurrent;
using System.Collections.Generic;
using Chronos.Hashflare.Events;
using QuickGraph.Serialization;
using ZES.Infrastructure.Projections;
using ZES.Interfaces;
using ZES.Interfaces.Domain;
using ZES.Interfaces.EventStore;
using ZES.Interfaces.Pipes;

namespace Chronos.Hashflare.Queries
{
    public class ContractStatsProjection : SingleProjection<ContractStatsProjection.Results>
    {
        public ContractStatsProjection(IEventStore<IAggregate> eventStore, ILog log, ITimeline timeline, IMessageQueue messageQueue) : base(eventStore, log, timeline, messageQueue)
        {
            State = new Results();
            Register<HashrateBought>(When);
            Register<AmountMinedByContract>(When);
        }

        private static Results When(HashrateBought e, Results state)
        {
            state.SetType(e.TxId, e.Type);
            return state;
        }

        private static Results When(AmountMinedByContract e, Results state)
        {
            state.AddMined(e.TxId, e.Quantity);
            return state;
        }
        
        public class Results
        {
            private readonly ConcurrentDictionary<string, double> _mined = new ConcurrentDictionary<string, double>();
            private readonly ConcurrentDictionary<string, string> _types = new ConcurrentDictionary<string, string>();

            public string Type(string txId) => _types.TryGetValue(txId, out var type) ? type : string.Empty;

            public void SetType(string txId, string type)
            {
                _types[txId] = type;
            }
            
            public double Mined(string txId)
            {
                return _mined.TryGetValue(txId, out var mined) ? mined : 0.0;
            }

            public void AddMined(string txId, double quantity)
            {
                var q = _mined.GetOrAdd(txId, 0);
                _mined[txId] = q + quantity;
            }
        }
    }
}