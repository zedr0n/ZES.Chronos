using System.Collections.Generic;
using Chronos.Hashflare.Commands;
using Chronos.Hashflare.Events;
using Stateless;
using ZES.Infrastructure.Sagas;

namespace Chronos.Hashflare.Sagas
{
    public class ContractSaga : StatelessSaga<ContractSaga.State, ContractSaga.Trigger>
    {
        private long _expiry;
        private int _quantity;
        private string _txId;
        public ContractSaga()
        {
            Register<HashrateBought>(e => e.Type == "SHA-256" ? e.TxId : null, Trigger.ContractCreated, e =>
            {
                _expiry = e.Timestamp;
                long dt = 365 * 24 * 60 * 60;
                dt *= 1000;
                _expiry += dt;

                _quantity = e.Quantity;
                _txId = e.TxId;
            });    
        }
         
        public enum Trigger
        {
            ContractCreated
        }
        
        public enum State 
        {
            Open,
            Complete
        }

        protected override void ConfigureStateMachine()
        {
            StateMachine = new StateMachine<State, Trigger>(State.Open);

            StateMachine.Configure(State.Open)
                .Permit(Trigger.ContractCreated, State.Complete);

            StateMachine.Configure(State.Complete)
                .Ignore(Trigger.ContractCreated)
                .OnEntry(() => SendCommand(new ExpireContract(_txId, "SHA-256", _quantity, _expiry)));
            base.ConfigureStateMachine();
        }
    }
}