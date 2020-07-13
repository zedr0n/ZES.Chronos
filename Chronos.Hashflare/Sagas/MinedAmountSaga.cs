using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Chronos.Hashflare.Commands;
using Chronos.Hashflare.Events;
using Stateless;
using ZES.Infrastructure.Sagas;

#pragma warning disable 1591

namespace Chronos.Hashflare.Sagas
{
    /// <inheritdoc />
    public class MinedAmountSaga : StatelessSaga<MinedAmountSaga.State, MinedAmountSaga.Trigger>
    {
        private readonly Dictionary<string, double> _contracts = new Dictionary<string, double>();
        private double _quantity;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="MinedAmountSaga"/> class.
        /// </summary>
        public MinedAmountSaga()
        {
            Register<ContractCreated>(e => "MinedAmountSaga", Trigger.ContractCreated, AddHashrate);
            Register<CoinMined>(e => "MinedAmountSaga", Trigger.MinedAmountAdded, e => _quantity = e.Quantity);
        }

        /// <inheritdoc />
        public enum Trigger
        {
            ContractCreated,
            MinedAmountAdded,
            Completed
        }

        /// <inheritdoc />
        public enum State
        {
            Open,
            Active,
            Complete
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Dictionary<string, double> Contracts
        {
            get
            {
                Hash(_contracts.Values.ToList());
                return _contracts;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private double Quantity
        {
            get
            {
                Hash(_quantity);
                return _quantity;
            }
        }
        
        /// <inheritdoc />
        protected override void ConfigureStateMachine()
        {
            StateMachine = new StateMachine<State, Trigger>(State.Open);

            StateMachine.Configure(State.Open)
                .Permit(Trigger.ContractCreated, State.Active);

            StateMachine.Configure(State.Active)
                .PermitReentry(Trigger.ContractCreated)
                .Permit(Trigger.MinedAmountAdded, State.Complete);
            
            StateMachine.Configure(State.Complete)
                .Permit(Trigger.Completed, State.Active)
                .OnEntry(() =>
                {
                    var total = Contracts.Values.Sum();
                    foreach (var c in Contracts)
                        SendCommand(new AddMinedCoinToContract(c.Key, string.Empty, c.Value / total * Quantity));
                    StateMachine.Fire(Trigger.Completed);
                });
            base.ConfigureStateMachine();
        }
        
        private void AddHashrate(ContractCreated e)
        {
            _contracts.TryGetValue(e.ContractId, out var quantity);
            quantity += e.Quantity; 
            _contracts[e.ContractId] = quantity;
        }
    }
}