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
    /// <inheritdoc />
    public class MinedAmountSaga : StatelessSaga<MinedAmountSaga.State, MinedAmountSaga.Trigger>
    {
        private readonly Dictionary<string, double> _contracts = new Dictionary<string, double>();
        private double _quantity;
        private string _type;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="MinedAmountSaga"/> class.
        /// </summary>
        public MinedAmountSaga()
        {
            Register<ContractCreated>(e => $"MinedAmountSaga[{e.Type}]", Trigger.ContractCreated, AddHashrate);
            Register<CoinMined>(e => $"MinedAmountSaga[{e.Type}]", Trigger.MinedAmountAdded, e =>
            {
                Quantity = e.Quantity;
            });
        }

        public enum Trigger
        {
            ContractCreated,
            MinedAmountAdded,
            Completed,
        }

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
                AddHash(_contracts.Values.ToList());
                return _contracts;
            }
        }

        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private double Quantity
        {
            get
            {
                AddHash(_quantity);
                return _quantity;
            }
            set
            {
                AddHash(value);
                _quantity = value;
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
                .Permit(Trigger.MinedAmountAdded, State.Complete);
            
            StateMachine.Configure(State.Complete)
                .Permit(Trigger.Completed, State.Active)
                .OnEntry(() =>
                {
                    var total = Contracts.Values.Sum();
                    foreach (var c in Contracts)
                        SendCommand(new AddMinedCoinToContract(c.Key, _type, c.Value / total * Quantity));
                    StateMachine.Fire(Trigger.Completed);
                });
        }
        
        private void AddHashrate(ContractCreated e)
        {
            _type = e.Type;
            _contracts.TryGetValue(e.ContractId, out var quantity);
            quantity += e.Quantity; 
            _contracts[e.ContractId] = quantity;
        }
    }
}