using Chronos.Coins.Commands;
using Chronos.Coins.Events;
using Chronos.Core;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.Utils;

namespace Chronos.Coins.Sagas
{
    /// <summary>
    /// Wallet transfer saga
    /// </summary>
    public class WalletTransferSaga : StatelessSaga<WalletTransferSaga.State, WalletTransferSaga.Trigger>
    {
        private Quantity _fee;
        private Quantity _quantity;
        private string _fromAddress;
        private string _toAddress;

        /// <summary>
        /// Initializes a new instance of the <see cref="WalletTransferSaga"/> class.
        /// </summary>
        public WalletTransferSaga()
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
        
        /// <summary>
        /// States
        /// </summary>
        public enum State
        {
            Open,
            Transferring,
            Confirmed,
            Completed,
        }

        /// <summary>
        /// Triggers
        /// </summary>
        public enum Trigger
        {
            StartTransfer,
            FeePaid,
            ConfirmationReceived,
            BalanceUpdated,
        }

        /// <inheritdoc/>
        protected override void ConfigureStateMachine()
        {
            base.ConfigureStateMachine();
            StateMachine.Configure(State.Open)
                .Permit(Trigger.StartTransfer, State.Transferring);
            StateMachine.Configure(State.Transferring)
                .OnEntry(() => SendCommand(new ChangeWalletBalance(_fromAddress, new Quantity(-_fee.Amount, _fee.Denominator), Id)))
                .Permit(Trigger.FeePaid, State.Confirmed);
            StateMachine.Configure(State.Confirmed)
                .OnEntry(() =>
                {
                    SendCommand(new ChangeWalletBalance(_fromAddress, new Quantity(-_quantity.Amount, _quantity.Denominator), Id));
                    SendCommand(new ChangeWalletBalance(_toAddress, _quantity, Id));
                })
                .Permit(Trigger.BalanceUpdated, State.Completed);
        }
    }
}