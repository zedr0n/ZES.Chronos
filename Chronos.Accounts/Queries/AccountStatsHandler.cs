using Chronos.Accounts.Events;
using Chronos.Core;
using Chronos.Core.Events;

namespace Chronos.Accounts.Queries
{
    public class AccountStatsHandler : ZES.Interfaces.Domain.IProjectionHandler<AccountStatsState>
    {
        public AccountStatsState Handle (ZES.Interfaces.IEvent e, AccountStatsState state)
        {
            return Handle((dynamic) e, state);;
        }

        public AccountStatsState Handle(AccountCreated e, AccountStatsState state)
        {
            var newState = new AccountStatsState(state);
            newState.AccountName = e.Name;
            return newState;
        }
        
        public AccountStatsState Handle (AssetDeposited e, AccountStatsState state)
        {
            var newState = new AccountStatsState(state);
            newState.Add(e.Quantity, e.Timestamp);
            return newState;
        }

        public AccountStatsState Handle(TransactionAdded e, AccountStatsState state)
        {
            var newState = new AccountStatsState(state);
            newState.Transactions.Add(e.TxId);
            return newState;
        }

        public AccountStatsState Handle(StockSplitAdded e, AccountStatsState state)
        {
            var newState = new AccountStatsState(state);
            newState.AddSplit(e.ForAsset, e.Timestamp, e.Ratio);
            return newState;
        }

        public AccountStatsState Handle(AssetTransactionStarted e, AccountStatsState state)
        {
            var newState = new AccountStatsState(state);
            newState.AddCost(e.Asset, e.Cost, e.Timestamp);
            return newState;
        }

        public AccountStatsState Handle(TransferStarted e, AccountStatsState state)
        {
            var newState = new AccountStatsState(state);
            newState.AddAssetTransfer(e.FromAccount, e.ToAccount, e.Amount, e.Timestamp);
            return newState;
        }
    }
}