using Chronos.Coins.Commands;
using Chronos.Coins.Events;
using Chronos.Core;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.Utils;

namespace Chronos.Coins.Sagas
{
    /// <summary>
    /// Mine -> Balance saga
    /// </summary>
    public class MiningSaga : StatelessSaga<MiningSaga.State, MiningSaga.Trigger>
    {
        private Quantity _quantity;
        private string _blockHash;

        /// <summary>
        /// Initializes a new instance of the <see cref="MiningSaga"/> class.
        /// </summary>
        public MiningSaga()
        {
            Register<CoinMined>(e => e.AggregateRootId(), Trigger.CoinMined, e =>
            {
                _quantity = e.MineQuantity;
                _blockHash = e.BlockHash;
            });
            Register<WalletBalanceChanged>(e => e.AggregateRootId(), Trigger.BalanceChanged);
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
                .OnEntry(() => SendCommand(new ChangeWalletBalance(Id, _quantity, _blockHash)));
        }
    }
}