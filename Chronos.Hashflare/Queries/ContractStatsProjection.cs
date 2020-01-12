using System.Collections.Concurrent;
using System.Collections.Generic;
using Chronos.Hashflare.Events;
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
            Register<ContractRatioAdjusted>(When);
        }

        private static Results When(ContractRatioAdjusted e, Results state)
        {
            state.SetRatio(e.TxId, e.Ratio);
            return state;
        }
        
        public class Results
        {
            private readonly ConcurrentDictionary<string, double> _ratios = new ConcurrentDictionary<string, double>(); 

            public double Ratio(string txId)
            {
                return _ratios.TryGetValue(txId, out var ratio) ? ratio : -1.0;
            }

            public void SetRatio(string txId, double ratio)
            {
                _ratios[txId] = ratio;
            }
        }
    }
}