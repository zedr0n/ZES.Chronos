using System.Collections.Generic;
using System.Linq;
using Chronos.Hashflare.Commands;
using Chronos.Hashflare.Events;
using Stateless;
using ZES.Infrastructure.Sagas;

namespace Chronos.Hashflare.Sagas
{
    public class MinedAmountSaga : StatelessSaga<MinedAmountSaga.State, MinedAmountSaga.Trigger>
    {
        private readonly Dictionary<string, double> _contracts = new Dictionary<string, double>();
        private double _quantity;
        public MinedAmountSaga()
        {
            Register<HashrateBought>(e => "MinedAmountSaga", Trigger.ContractCreated, AddHashrate);
            Register<AmountMined>(e => "MinedAmountSaga", Trigger.MinedAmountAdded, e => _quantity = e.Quantity);
        }

        private void AddHashrate(HashrateBought e)
        {
            _contracts.TryGetValue(e.TxId, out var quantity);
            quantity += e.Quantity; 
            _contracts[e.TxId] = quantity;
        }
        
        public enum Trigger
        {
            ContractCreated,
            MinedAmountAdded,
            Completed
        }

        public enum State
        {
            Open,
            Active,
            Complete
        }

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
                    var total = _contracts.Values.Sum();
                    foreach (var c in _contracts)
                        SendCommand(new AddMinedToContract(c.Key, string.Empty, c.Value / total * _quantity));
                    StateMachine.Fire(Trigger.Completed);
                });
            base.ConfigureStateMachine();
        }
    }
}