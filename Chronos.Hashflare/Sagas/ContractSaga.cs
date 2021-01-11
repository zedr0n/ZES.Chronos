using Chronos.Hashflare.Events;
using NodaTime;
using Stateless;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.Utils;

#pragma warning disable 1591

namespace Chronos.Hashflare.Sagas
{
    /// <summary>
    /// Contract saga
    /// </summary>
    public class ContractSaga : StatelessSaga<ContractSaga.State, ContractSaga.Trigger>
    {
        private Instant _expiry;
        private int _quantity;
        private string _txId;
        private string _type;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ContractSaga"/> class.
        /// </summary>
        public ContractSaga()
        {
            Register<ContractCreated>(e => e.AggregateRootId(), Trigger.ContractCreated, e =>
            {
                _expiry = e.Timestamp;
                _expiry += Duration.FromDays(365);

                _type = e.Type;
                _quantity = e.Quantity;
                _txId = e.ContractId;
            });    
        }
        
        /// <summary>
        /// Triggers
        /// </summary>
        public enum Trigger
        {
            ContractCreated,
        }
        
        /// <summary>
        /// States
        /// </summary>
        public enum State 
        {
            Open,
            Complete,
        }

        /// <inheritdoc />
        protected override void ConfigureStateMachine()
        {
            base.ConfigureStateMachine();

            StateMachine.Configure(State.Open)
                .Permit(Trigger.ContractCreated, State.Complete);

            StateMachine.Configure(State.Complete)
                .Ignore(Trigger.ContractCreated);
        }
    }
}