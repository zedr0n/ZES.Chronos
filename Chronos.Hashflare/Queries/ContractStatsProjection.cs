using System.Collections.Concurrent;
using Chronos.Hashflare.Events;
using ZES.Infrastructure.Projections;
using ZES.Interfaces;
using ZES.Interfaces.Domain;
using ZES.Interfaces.EventStore;
using ZES.Interfaces.Pipes;

namespace Chronos.Hashflare.Queries
{
    /// <inheritdoc />
    public class ContractStatsProjection : SingleProjection<ContractStatsProjection.Results>
    {
        /// <inheritdoc />
        public ContractStatsProjection(IEventStore<IAggregate> eventStore, ILog log, ITimeline timeline, IMessageQueue messageQueue)
            : base(eventStore, log, timeline, messageQueue)
        {
            State = new Results();
            Register<ContractCreated>(When);
            Register<CoinMinedByContract>(When);
        }

        private static Results When(ContractCreated e, Results state)
        {
            state.SetType(e.ContractId, e.Type);
            return state;
        }

        private static Results When(CoinMinedByContract e, Results state)
        {
            state.AddMined(e.ContractId, e.Quantity);
            return state;
        }
        
        /// <summary>
        /// Contract stats results class
        /// </summary>
        public class Results
        {
            private readonly ConcurrentDictionary<string, double> _mined = new ConcurrentDictionary<string, double>();
            private readonly ConcurrentDictionary<string, string> _types = new ConcurrentDictionary<string, string>();

            /// <summary>
            /// Gets the hash type of the contract 
            /// </summary>
            /// <param name="contractId">Contract id</param>
            /// <returns>Hash type of the contract</returns>
            public string Type(string contractId) 
                => _types.TryGetValue(contractId, out var type) ? type : string.Empty;

            /// <summary>
            /// Sets the type of the contract
            /// </summary>
            /// <param name="contractId">Contract id</param>
            /// <param name="type">Hash type</param>
            public void SetType(string contractId, string type)
            {
                _types[contractId] = type;
            }
            
            /// <summary>
            /// Gets the amount mined by the contract
            /// </summary>
            /// <param name="contractId">Contract id</param>
            /// <returns>Total amount mined</returns>
            public double Mined(string contractId)
            {
                return _mined.TryGetValue(contractId, out var mined) ? mined : 0.0;
            }

            /// <summary>
            /// Adds the mined amount
            /// </summary>
            /// <param name="contractId">Contract id</param>
            /// <param name="quantity">Hash rate quantity</param>
            public void AddMined(string contractId, double quantity)
            {
                var q = _mined.GetOrAdd(contractId, 0);
                _mined[contractId] = q + quantity;
            }
        }
    }
}