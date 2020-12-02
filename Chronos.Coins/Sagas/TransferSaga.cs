using Chronos.Coins.Commands;
using Chronos.Coins.Events;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.Utils;

namespace Chronos.Coins.Sagas
{
    public class TransferSaga : StatelessSaga<TransferSaga.State, TransferSaga.Trigger>
    {
        private double _fee;
        private double _quantity;
        private string _fromAddress;
        private string _toAddress;
        
        public TransferSaga()
        {
            InitialState = State.Open;
            Register<CoinsTransferred>(e => e.TxId, Trigger.StartTransfer, e =>
            {
                _fee = e.Fee;
                _fromAddress = e.FromAddress;
                _toAddress = e.ToAddress;
                _quantity = e.Quantity;
            });
            Register<WalletBalanceChanged>(e => e.TxId, e => e.AggregateRootId() == _fromAddress ? Trigger.FeePaid : Trigger.BalanceUpdated);
        }
        
        public enum State
        {
            Open,
            Transferring,
            Confirmed,
            Completed,
        }

        public enum Trigger
        {
            StartTransfer,
            FeePaid,
            ConfirmationReceived,
            BalanceUpdated,
        }

        protected override void ConfigureStateMachine()
        {
            base.ConfigureStateMachine();
            StateMachine.Configure(State.Open)
                .Permit(Trigger.StartTransfer, State.Transferring);
            StateMachine.Configure(State.Transferring)
                .OnEntry(() => SendCommand(new ChangeWalletBalance(_fromAddress, -_fee, Id)))
                .Permit(Trigger.FeePaid, State.Confirmed);
            StateMachine.Configure(State.Confirmed)
                .OnEntry(() =>
                {
                    SendCommand(new ChangeWalletBalance(_fromAddress, -_quantity, Id));
                    SendCommand(new ChangeWalletBalance(_toAddress, _quantity, Id));
                })
                .Permit(Trigger.BalanceUpdated, State.Completed);
        }
    }
}