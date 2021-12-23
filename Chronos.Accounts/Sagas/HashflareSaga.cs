using Chronos.Accounts.Commands;
using Chronos.Accounts.Events;
using Chronos.Core;
using Chronos.Hashflare.Events;
using ZES.Infrastructure.Domain;

namespace Chronos.Accounts.Sagas
{
    /// <summary>
    /// Hashflare saga
    /// </summary>
    public class HashflareSaga : StatelessSaga<HashflareSaga.State, HashflareSaga.Trigger>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HashflareSaga"/> class.
        /// </summary>
        public HashflareSaga()
        {
            Register<HashflareRegistered>(e => "Hashflare", Trigger.Register);
            Register<AccountCreated>(Trigger.Created);
            RegisterWithParameters<CoinMined>(e => "Hashflare", Trigger.CoinMined);
            Register<AssetDeposited>(Trigger.AccountUpdated);
        }
       
        /// <summary>
        /// Triggers
        /// </summary>
        public enum Trigger
        {
            Register,
            Created,
            CoinMined,
            AccountUpdated,
        }

        /// <summary>
        /// States
        /// </summary>
        public enum State
        {
            Open,
            Creating,
            Created,
            Processing,
        }

        /// <inheritdoc/>
        protected override void ConfigureStateMachine()
        {
            base.ConfigureStateMachine();
            StateMachine.Configure(State.Open)
                .Permit(Trigger.Register, State.Creating);
            StateMachine.Configure(State.Creating)
                .OnEntry(() => SendCommand(new CreateAccount("Hashflare", AccountType.Saving)))
                .Permit(Trigger.Created, State.Created);
            StateMachine.Configure(State.Created)
                .Permit(Trigger.CoinMined, State.Processing);
            StateMachine.Configure(State.Processing)
                .OnEntryFrom(GetTrigger<CoinMined>(), e => SendCommand(new DepositAsset("Hashflare", new Quantity(e.Quantity, new Asset("Bitcoin", "BTC", Asset.Type.Coin)))))
                .Permit(Trigger.AccountUpdated, State.Created);
        }
    }
}