﻿using Chronos.Accounts.Commands;
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
        private string _fromAccount;
        private string _toAccount;
        private Quantity _quantity;

        private string _fromTxId;
        private string _toTxId;
        
        private int _accountsCompleted;

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountTransferSaga"/> class.
        /// </summary>
        public AccountTransferSaga()
        {
            Register<TransferStarted>(e => e.TxId, Trigger.TransferStarted, e =>
            {
                _fromAccount = e.FromAccount;
                _toAccount = e.ToAccount;
                _quantity = e.Amount;
                _fromTxId = $"{e.TxId}[From]";
                _toTxId = $"{e.TxId}[To]";
            });
            RegisterIf<TransactionAdded>(e => GetTransferTxId(e.TxId), e => Trigger.AccountUpdated, e => e.TxId == _fromTxId || e.TxId == _toTxId, e => _accountsCompleted++);
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
            StateMachine.Configure(State.Open)
                .Permit(Trigger.TransferStarted, State.Processing);
            StateMachine.Configure(State.Processing)
                .Permit(Trigger.TransferCompleted, State.Completed)
                .PermitReentry(Trigger.AccountUpdated)
                .OnEntry(() =>
                {
                    if (_accountsCompleted == 0)
                    {
                        SendCommand(new RecordTransaction(_fromTxId, new Quantity(-_quantity.Amount, _quantity.Denominator), Transaction.TransactionType.Transfer, $"Transfer to {_toAccount}"));
                        SendCommand(new AddTransaction(_fromAccount, _fromTxId));
                    }
                    else if (_accountsCompleted == 1)
                    {
                        SendCommand(new RecordTransaction(_toTxId, new Quantity(_quantity.Amount, _quantity.Denominator), Transaction.TransactionType.Transfer, $"Transfer from {_fromAccount}"));
                        SendCommand(new AddTransaction(_toAccount, _toTxId));
                    }
                    else
                    {
                        StateMachine.Fire(Trigger.TransferCompleted);
                    }
                });
        }

        private string GetTransferTxId(string txId) => txId.Replace("[To]", string.Empty).Replace($"[From]", string.Empty);
    }
}