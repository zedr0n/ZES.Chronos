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
        /// <summary>
        /// Initializes a new instance of the <see cref="WalletSaga"/> class.
        /// </summary>
        public WalletSaga()
        {
            RegisterWithParameters<WalletCreated>(e => e.Address, Trigger.WalletCreated);
            Register<AccountCreated>(Trigger.AccountCreated);
            RegisterWithParameters<WalletBalanceChanged>(e => e.AggregateRootId(), Trigger.BalanceChanged);
            RegisterWithParameters<TransactionRecorded>(Trigger.TransactionRecorded);
            Register<TransactionAdded>(Trigger.AccountUpdated);
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
                .OnEntryFrom(
                    GetTrigger<WalletCreated>(),
                    e => SendCommand(new CreateAccount(e.Address, AccountType.Trading)));
            
            StateMachine.Configure(State.Listening)
                .Permit(Trigger.BalanceChanged, State.RecordingTransaction);
            StateMachine.Configure(State.RecordingTransaction)
                .Permit(Trigger.TransactionRecorded, State.UpdatingAccount)
                .OnEntryFrom(
                    GetTrigger<WalletBalanceChanged>(),
                    e => SendCommand(new RecordTransaction($"{Id}[{e.TxId}]", e.Delta, Transaction.TransactionType.Unknown, string.Empty)));

            StateMachine.Configure(State.UpdatingAccount)
                .Permit(Trigger.AccountUpdated, State.Listening)
                .OnEntryFrom(
                    GetTrigger<TransactionRecorded>(),
                    e => SendCommand(new AddTransaction(Id, e.TxId)));
        }
    }
}