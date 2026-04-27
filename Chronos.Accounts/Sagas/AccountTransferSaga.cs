using Chronos.Accounts.Commands;
using Chronos.Accounts.Events;
using Chronos.Core;
using Chronos.Core.Commands;
using ZES.Infrastructure.Domain;

namespace Chronos.Accounts.Sagas
{
    /// <summary>
    /// Transfer -> Transactions saga
    /// </summary>
    public class AccountTransferSaga : StatelessSaga<AccountTransferSaga.State, AccountTransferSaga.Trigger>
    {
        private int _accountsCompleted;

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountTransferSaga"/> class.
        /// </summary>
        public AccountTransferSaga()
        {
            RegisterWithParameters<TransferStarted>(e => e.TxId, Trigger.TransferStarted);
            RegisterWithParameters<TransactionAdded>(Trigger.AccountUpdated);
        }
        
        /// <summary>
        /// Triggers
        /// </summary>
        public enum Trigger
        {
            TransferStarted,
            AccountUpdated,
            TransferCompleted,
        }

        /// <summary>
        /// States
        /// </summary>
        public enum State
        {
            Open, 
            Processing,
            Completed,
        }

        /// <inheritdoc/>
        protected override void ConfigureStateMachine()
        {
            base.ConfigureStateMachine();

            var transferTrigger = GetTrigger<TransferStarted>();
            var txTrigger = GetTrigger<TransactionAdded>();
            
            StateMachine.Configure(State.Open)
                .Permit(Trigger.TransferStarted, State.Processing);
            StateMachine.Configure(State.Processing)
                .Permit(Trigger.TransferCompleted, State.Completed)
                .PermitReentry(Trigger.AccountUpdated)
                .OnEntryFrom(transferTrigger, Handle)
                .OnEntryFrom(txTrigger, Handle);
        }

        private void Handle(TransferStarted e)
        {
            var fromTxId = $"{e.TxId}[From]";
            var toTxId = $"{e.TxId}[To]";

            if (e.Amount.Denominator.AssetType == AssetType.Currency)
            {
                SendCommand(new CreateTransaction(fromTxId, new Quantity(-e.Amount.Amount, e.Amount.Denominator), Transaction.TransactionType.Transfer, $"Transfer to {e.ToAccount}",null, e.ToAccount));
                SendCommand(new AddTransaction(e.FromAccount, fromTxId));
                SendCommand(new CreateTransaction(toTxId, new Quantity(e.Amount.Amount, e.Amount.Denominator), Transaction.TransactionType.Transfer, $"Transfer from {e.FromAccount}", null, e.FromAccount));
                SendCommand(new AddTransaction(e.ToAccount, toTxId));
            }
            else
            {
                SendCommand(new DepositAsset(e.FromAccount, new Quantity(-e.Amount.Amount, e.Amount.Denominator)));
                SendCommand(new DepositAsset(e.ToAccount, new Quantity(e.Amount.Amount, e.Amount.Denominator)));
            }
        }

        private void Handle(TransactionAdded e)
        {
            _accountsCompleted++;
            Log.Info($"Transaction added: {e.TxId}, total : {_accountsCompleted}");

            if (_accountsCompleted == 2)
                StateMachine.Fire(Trigger.TransferCompleted);
        }
    }
}