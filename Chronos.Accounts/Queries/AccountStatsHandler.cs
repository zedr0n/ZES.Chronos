using Chronos.Accounts.Events;
using Chronos.Core;
using Chronos.Core.Events;
using ZES.Infrastructure.Utils;

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
            newState.AddCost(e.Asset, e.Cost, e.Fee, e.Timestamp);
            return newState;
        }

        public AccountStatsState Handle(QuoteAdded e, AccountStatsState state)
        {
            var newState = new AccountStatsState(state);
            newState.AddQuote(e.AggregateRootId(), e.Close, e.Timestamp);
            return newState;
        }

        public AccountStatsState Handle(TransferStarted e, AccountStatsState state)
        {
            var hasFeeDisposal = e.Fee != null && e.Fee.IsValid() && e.Fee.Amount != 0 && e.Fee.Denominator.AssetType != AssetType.Currency;

            if (e.Amount.Denominator.AssetType == AssetType.Currency && !hasFeeDisposal)
                return state;

            var newState = new AccountStatsState(state);
            if (e.Amount.Denominator.AssetType != AssetType.Currency)
                newState.AddAssetTransfer(e.FromAccount, e.ToAccount, e.Amount, e.Fee, e.Timestamp);
            if (hasFeeDisposal)
                newState.AddFeeDisposal(e.FromAccount, e.Fee, e.Timestamp);
            return newState;
        }
    }
}