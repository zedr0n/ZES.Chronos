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
        private string _fromAddress;
        private string _toAddress;
        private Quantity _quantity;

        /// <summary>
        /// Initializes a new instance of the <see cref="WalletTransferSaga"/> class.
        /// </summary>
        public WalletTransferSaga()
        {
            InitialState = State.Open;
            RegisterWithParameters<CoinsTransferred>(e => e.TxId, Trigger.StartTransfer);
            RegisterWithParameters<WalletBalanceChanged>(Trigger.BalanceUpdated);
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
            BalanceUpdated,
        }

        /// <inheritdoc/>
        protected override void ConfigureStateMachine()
        {
            base.ConfigureStateMachine();
            StateMachine.Configure(State.Open)
                .Permit(Trigger.StartTransfer, State.Transferring);
            StateMachine.Configure(State.Transferring)
                .Permit(Trigger.BalanceUpdated, State.Confirmed)
                .OnEntryFrom(
                    GetTrigger<CoinsTransferred>(),
                    e =>
                    {
                        SendCommand(new ChangeWalletBalance(e.FromAddress, new Quantity(-e.Fee.Amount, e.Fee.Denominator), Id));
                        _quantity = e.Quantity;
                        _fromAddress = e.FromAddress;
                        _toAddress = e.ToAddress;
                    });

            StateMachine.Configure(State.Confirmed)
                .Permit(Trigger.BalanceUpdated, State.Completed)
                .OnEntryFrom(
                    GetTrigger<WalletBalanceChanged>(),
                    e =>
                    {
                        SendCommand(new ChangeWalletBalance(_fromAddress, new Quantity(-_quantity.Amount, _quantity.Denominator), Id));
                        SendCommand(new ChangeWalletBalance(_toAddress, _quantity, Id));
                    });
        }
    }
}