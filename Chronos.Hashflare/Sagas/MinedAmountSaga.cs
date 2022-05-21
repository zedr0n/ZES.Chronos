using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Chronos.Hashflare.Commands;
using Chronos.Hashflare.Events;
using Stateless;
using ZES.Infrastructure.Domain;

#pragma warning disable 1591

namespace Chronos.Hashflare.Sagas
{
    /// <summary>
    /// Mined -> Contract saga
    /// </summary>
    public class MinedAmountSaga : StatelessSaga<MinedAmountSaga.State, MinedAmountSaga.Trigger>
    {
        private readonly Dictionary<string, double> _contracts = new Dictionary<string, double>();
        
        /// <summary>
        /// Initializes a new instance of the <see cref="MinedAmountSaga"/> class.
        /// </summary>
        public MinedAmountSaga()
        {
            RegisterWithParameters<ContractCreated>(e => $"MinedAmountSaga[{e.Type}]", Trigger.ContractCreated);
            RegisterWithParameters<CoinMined>(e => $"MinedAmountSaga[{e.Type}]", Trigger.MinedAmountAdded);
        }

        /// <summary>
        /// Triggers
        /// </summary>
        public enum Trigger
        {
            ContractCreated,
            MinedAmountAdded,
            Completed,
        }

        /// <summary>
        /// States
        /// </summary>
        public enum State
        {
            Open,
            Active,
            Complete,
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private Dictionary<string, double> Contracts
        {
            get
            {
                AddHashDoubleList(_contracts.Values.ToList());
                return _contracts;
            }
        }

        /// <inheritdoc />
        protected override void ConfigureStateMachine()
        {
            base.ConfigureStateMachine();

            StateMachine.Configure(State.Open)
                .Permit(Trigger.ContractCreated, State.Active);

            StateMachine.Configure(State.Active)
                .PermitReentry(Trigger.ContractCreated)
                .Permit(Trigger.MinedAmountAdded, State.Complete)
                .OnEntryFrom(GetTrigger<ContractCreated>(), AddHashrate);

            StateMachine.Configure(State.Complete)
                .Permit(Trigger.Completed, State.Active)
                .OnEntryFrom(GetTrigger<CoinMined>(), e =>
                {
                    var total = Contracts.Values.Sum();
                    foreach (var c in Contracts)
                        SendCommand(new AddMinedCoinToContract(c.Key, e.Type, c.Value / total * e.Quantity));
                    StateMachine.Fire(Trigger.Completed);
                });
        }
        
        private void AddHashrate(ContractCreated e)
        {
            _contracts.TryGetValue(e.ContractId, out var quantity);
            quantity += e.Quantity; 
            _contracts[e.ContractId] = quantity;
        }
    }
}