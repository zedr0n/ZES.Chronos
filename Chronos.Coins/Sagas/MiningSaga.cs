using Chronos.Coins.Commands;
using Chronos.Coins.Events;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.Utils;

namespace Chronos.Coins.Sagas
{
    /// <summary>
    /// Mine -> Balance saga
    /// </summary>
    public class MiningSaga : StatelessSaga<MiningSaga.State, MiningSaga.Trigger>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MiningSaga"/> class.
        /// </summary>
        public MiningSaga()
        {
            RegisterWithParameters<CoinMined>(e => e.AggregateRootId(), Trigger.CoinMined);
            RegisterWithParameters<WalletBalanceChanged>(Trigger.BalanceChanged);
        }
        
        /// <summary>
        /// States
        /// </summary>
        public enum State
        {
            Open,
            Processing,
            Complete,
        }

        /// <summary>
        /// Triggers
        /// </summary>
        public enum Trigger
        {
            CoinMined,
            BalanceChanged,
        }

        /// <inheritdoc/>
        protected override void ConfigureStateMachine()
        {
            base.ConfigureStateMachine();
            StateMachine.Configure(State.Open)
                .Permit(Trigger.CoinMined, State.Processing);
            StateMachine.Configure(State.Processing)
                .Permit(Trigger.BalanceChanged, State.Complete)
                .OnEntryFrom(GetTrigger<CoinMined>(), e => SendCommand(new ChangeWalletBalance(Id, e.MineQuantity, e.BlockHash)));
        }
    }
}