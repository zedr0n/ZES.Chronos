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
        /// <summary>
        /// Initializes a new instance of the <see cref="AccountTransferSaga"/> class.
        /// </summary>
        public AccountTransferSaga()
        {
            RegisterWithParameters<TransferStarted>(e => e.TxId, Trigger.TransferStarted);
        }
        
        /// <summary>
        /// Triggers
        /// </summary>
        public enum Trigger
        {
            TransferStarted,
        }

        /// <summary>
        /// States
        /// </summary>
        public enum State
        {
            Open, 
            Processing,
        }

        /// <inheritdoc/>
        protected override void ConfigureStateMachine()
        {
            base.ConfigureStateMachine();

            var transferTrigger = GetTrigger<TransferStarted>();
            
            StateMachine.Configure(State.Open)
                .Permit(Trigger.TransferStarted, State.Processing);
            StateMachine.Configure(State.Processing)
                .OnEntryFrom(transferTrigger, Handle);
        }

        private void Handle(TransferStarted e)
        {
            var feeTxId = $"{e.TxId}[Fee]";
            var fromTxId = $"{e.TxId}[From]";
            var toTxId = $"{e.TxId}[To]";

            var transferQuantity = e.Amount.Copy();
            
            if (e.Fee != null && e.Fee.IsValid() && e.Fee.Amount != 0)
            {
                if (e.Fee.Denominator.AssetType == AssetType.Currency)
                {
                    SendCommand(new CreateTransaction(feeTxId, e.Fee with { Amount = -e.Fee.Amount }, Transaction.TransactionType.Fee, $"Fee for transfer to {e.ToAccount}", e.Amount.Denominator.AssetId, e.ToAccount));
                    SendCommand(new AddTransaction(e.FromAccount, feeTxId));
                }
                else
                {
                    SendCommand(new DepositAsset(e.FromAccount, e.Fee with { Amount = -e.Fee.Amount }));
                }
                
                // subtract fee from transfer amount if it's the same asset/currency
                if (e.Fee.Denominator == e.Amount.Denominator)
                    transferQuantity -= e.Fee;
            }

            if (e.Amount.Denominator.AssetType == AssetType.Currency)
            {
                SendCommand(new CreateTransaction(fromTxId, transferQuantity*(-1), Transaction.TransactionType.Transfer, $"Transfer to {e.ToAccount}",null, e.ToAccount));
                SendCommand(new AddTransaction(e.FromAccount, fromTxId));
                SendCommand(new CreateTransaction(toTxId, transferQuantity, Transaction.TransactionType.Transfer, $"Transfer from {e.FromAccount}", null, e.FromAccount));
                SendCommand(new AddTransaction(e.ToAccount, toTxId));
            }
            else
            {
                SendCommand(new DepositAsset(e.FromAccount, transferQuantity with { Amount = -transferQuantity.Amount } ));
                SendCommand(new DepositAsset(e.ToAccount, transferQuantity));
            }
        }
    }
}