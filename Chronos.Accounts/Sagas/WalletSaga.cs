using System;
using System.Collections.Generic;
using Chronos.Accounts.Commands;
using Chronos.Accounts.Events;
using Chronos.Coins.Events;
using Chronos.Core;
using Chronos.Core.Commands;
using Chronos.Core.Events;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.Utils;

namespace Chronos.Accounts.Sagas
{
    /// <summary>
    /// Wallet balance change -> Transaction saga
    /// </summary>
    public class WalletSaga : StatelessSaga<WalletSaga.State, WalletSaga.Trigger>
    {
        private string _accountName;
        private Quantity _delta;
        private string _txId;

        /// <summary>
        /// Initializes a new instance of the <see cref="WalletSaga"/> class.
        /// </summary>
        public WalletSaga()
        {
            Register<WalletCreated>(e => e.Address, Trigger.WalletCreated, e => _accountName = e.Address);
            Register<AccountCreated>(e => e.Name, Trigger.AccountCreated);
            Register<WalletBalanceChanged>(e => e.AggregateRootId(), Trigger.BalanceChanged, e =>
            {
                _delta = e.Delta;
                _txId = e.TxId;
            });
            Register<TransactionRecorded>(e => GetAccountNameFromTxId(e.TxId), Trigger.TransactionRecorded);
            Register<TransactionAdded>(e => e.AggregateRootId(), Trigger.AccountUpdated);
        }
        
        /// <summary>
        /// States
        /// </summary>
        public enum State
        {
            Open,
            Creating,
            Listening,
            RecordingTransaction,
            UpdatingAccount,
        }

        /// <summary>
        /// Triggers
        /// </summary>
        public enum Trigger
        {
            WalletCreated,
            AccountCreated,
            BalanceChanged,
            TransactionRecorded,
            AccountUpdated,
        }

        /// <inheritdoc/>
        protected override void ConfigureStateMachine()
        {
            base.ConfigureStateMachine();
            StateMachine.Configure(State.Open)
                .Permit(Trigger.WalletCreated, State.Creating);
            StateMachine.Configure(State.Creating)
                .Permit(Trigger.AccountCreated, State.Listening)
                .OnEntry(() => SendCommand(new CreateAccount(_accountName, AccountType.Trading)));
            StateMachine.Configure(State.Listening)
                .Permit(Trigger.BalanceChanged, State.RecordingTransaction);
            StateMachine.Configure(State.RecordingTransaction)
                .Permit(Trigger.TransactionRecorded, State.UpdatingAccount)
                .OnEntry(() => SendCommand(new RecordTransaction($"{_accountName}[{_txId}]", _delta, Transaction.TransactionType.Unknown, string.Empty)));
            StateMachine.Configure(State.UpdatingAccount)
                .Permit(Trigger.AccountUpdated, State.Listening)
                .OnEntry(() => SendCommand(new AddTransaction(_accountName, $"{_accountName}[{_txId}]")));
        }

        private string GetAccountNameFromTxId(string txId) => txId.Contains("[") ? txId.Substring(0, txId.IndexOf("[", StringComparison.Ordinal)) : null;
    }
}