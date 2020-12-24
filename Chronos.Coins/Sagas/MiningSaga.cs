using Chronos.Coins.Commands;
using Chronos.Coins.Events;
using Chronos.Core;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.Utils;

namespace Chronos.Coins.Sagas
{
    public class MiningSaga : StatelessSaga<MiningSaga.State, MiningSaga.Trigger>
    {
        private Quantity _quantity;
        private int _mineCount;
        
        public MiningSaga()
        {
            Register<CoinMined>(e => e.AggregateRootId(), Trigger.CoinMined, e =>
            {
                _quantity = e.MineQuantity;
                _mineCount++;
            });
            Register<WalletBalanceChanged>(e => e.AggregateRootId(), Trigger.BalanceChanged);
        }
        
        public enum State
        {
            Open,
            Processing,
            Complete,
        }

        public enum Trigger
        {
            CoinMined,
            BalanceChanged,
        }

        protected override void ConfigureStateMachine()
        {
            base.ConfigureStateMachine();
            StateMachine.Configure(State.Open)
                .Permit(Trigger.CoinMined, State.Processing);
            StateMachine.Configure(State.Processing)
                .Permit(Trigger.BalanceChanged, State.Complete)
                .OnEntry(() => SendCommand(new ChangeWalletBalance(Id, _quantity, $"Mine{_mineCount}")));
        }
    }
}