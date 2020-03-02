using System.Collections.Generic;
using Chronos.Hashflare.Commands;
using Chronos.Hashflare.Events;
using Stateless;
using ZES.Infrastructure.Sagas;

#pragma warning disable 1591

namespace Chronos.Hashflare.Sagas
{
    /// <inheritdoc />
    public class ContractSaga : StatelessSaga<ContractSaga.State, ContractSaga.Trigger>
    {
        private long _expiry;
        private int _quantity;
        private string _txId;
        private string _type;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ContractSaga"/> class.
        /// </summary>
        public ContractSaga()
        {
            Register<ContractCreated>(e => e.ContractId, Trigger.ContractCreated, e =>
            {
                _expiry = e.Timestamp;
                long dt = 365 * 24 * 60 * 60;
                dt *= 1000;
                _expiry += dt;

                _type = e.Type;
                _quantity = e.Quantity;
                _txId = e.ContractId;
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

        /// <inheritdoc />
        protected override void ConfigureStateMachine()
        {
            StateMachine = new StateMachine<State, Trigger>(State.Open);

            StateMachine.Configure(State.Open)
                .Permit(Trigger.ContractCreated, State.Complete);

            StateMachine.Configure(State.Complete)
                .Ignore(Trigger.ContractCreated);
            base.ConfigureStateMachine();
        }
    }
}