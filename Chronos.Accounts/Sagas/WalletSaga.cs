using System;
using Chronos.Accounts.Commands;
using Chronos.Accounts.Events;
using Chronos.Coins.Events;
using Chronos.Core;
using Chronos.Core.Commands;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.Utils;

namespace Chronos.Accounts.Sagas
{
    public class WalletSaga : StatelessSaga<WalletSaga.State, WalletSaga.Trigger>
    {
        private string _accountName;
        private Quantity _delta;
        private string _txId;
        
        public WalletSaga()
        {
            Register<WalletCreated>(e => e.Address, Trigger.WalletCreated, e => _accountName = e.Address);
            Register<AccountCreated>(e => e.Name, Trigger.AccountCreated);
            Register<WalletBalanceChanged>(e => e.AggregateRootId(), Trigger.BalanceChanged, e =>
            {
                _delta = e.Delta;
                _txId = e.TxId;
            });
            Register<TransactionAdded>(e => GetAccountNameFromTxId(e.TxId), Trigger.AccountUpdated);
        }
        
        public enum State
        {
            Open,
            Creating,
            Listening,
            UpdatingAccount,
        }

        public enum Trigger
        {
            WalletCreated,
            AccountCreated,
            BalanceChanged,
            AccountUpdated,
        }

        protected override void ConfigureStateMachine()
        {
            base.ConfigureStateMachine();
            StateMachine.Configure(State.Open)
                .Permit(Trigger.WalletCreated, State.Creating);
            StateMachine.Configure(State.Creating)
                .Permit(Trigger.AccountCreated, State.Listening)
                .OnEntry(() => SendCommand(new CreateAccount(_accountName, AccountType.Trading)));
            StateMachine.Configure(State.Listening)
                .Permit(Trigger.BalanceChanged, State.UpdatingAccount);
            StateMachine.Configure(State.UpdatingAccount)
                .Permit(Trigger.AccountUpdated, State.Listening)
                .OnEntry(() =>
                {
                    SendCommand(new RecordTransaction($"{_accountName}[{_txId}]", _delta, Transaction.TransactionType.Unknown, "Mining"));
                    SendCommand(new AddTransaction(_accountName, $"{_accountName}[{_txId}]"));
                });
        }

        private string GetAccountNameFromTxId(string txId) => txId.Substring(0, txId.Length - txId.IndexOf("Mine", StringComparison.InvariantCultureIgnoreCase));
    }
}