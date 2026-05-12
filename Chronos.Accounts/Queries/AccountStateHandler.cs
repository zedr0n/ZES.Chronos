using System;
using Chronos.Accounts.Events;
using Chronos.Core;
using Chronos.Core.Events;
using ZES.Infrastructure.Utils;

namespace Chronos.Accounts.Queries
{
    public class AccountStateHandler : ZES.Interfaces.Domain.IProjectionHandler<AccountState>
    {
        public AccountState Handle (ZES.Interfaces.IEvent e, AccountState state)
        {
            return Handle((dynamic) e, state);;
        }

        public AccountState Handle(AccountCreated e, AccountState state)
        {
            var newState = new AccountState(state);
            newState.AccountName = e.Name;
            return newState;
        }
        
        public AccountState Handle (AssetDeposited e, AccountState state)
        {
            var newState = new AccountState(state);
            newState.Add(e.Quantity, e.Timestamp);
            return newState;
        }

        public AccountState Handle(TransactionAdded e, AccountState state)
        {
            var newState = new AccountState(state);
            newState.Transactions.Add(e.TxId);
            return newState;
        }

        public AccountState Handle(StockSplitAdded e, AccountState state)
        {
            var newState = new AccountState(state);
            newState.AddSplit(e.ForAsset, e.Timestamp, e.Ratio);
            return newState;
        }

        public AccountState Handle(AssetTransactionStarted e, AccountState state)
        {
            var newState = new AccountState(state);
            newState.AddCost(e.Asset, e.Cost, e.Fee, e.Timestamp, e.RetroactiveId?.Id ?? e.CommandId?.Id);
            if (e.AssetTransactionType == AssetTransactionType.Income && e.Cost.Denominator.AssetType == AssetType.Currency)
                newState.AddIncome(e.Cost, e.Timestamp);
            if (e.AssetTransactionType == AssetTransactionType.Spend)
            {
                if(e.Cost.Denominator.AssetType != AssetType.Currency)
                    throw new InvalidOperationException("Spend transactions must be denominated in currency assets");
                
                newState.AddSpend(e.Cost, e.Timestamp);
            }

            return newState;
        }

        public AccountState Handle(QuoteAdded e, AccountState state)
        {
            var newState = new AccountState(state);
            newState.AddQuote(e.AggregateRootId(), e.Close, e.Timestamp);
            return newState;
        }

        public AccountState Handle(TransferStarted e, AccountState state)
        {
            var hasFeeDisposal = e.Fee != null && e.Fee.IsValid() && e.Fee.Amount != 0 && e.Fee.Denominator.AssetType != AssetType.Currency;

            if (e.Amount.Denominator.AssetType == AssetType.Currency && !hasFeeDisposal)
                return state;

            var newState = new AccountState(state);
            if (e.Amount.Denominator.AssetType != AssetType.Currency)
                newState.AddAssetTransfer(e.FromAccount, e.ToAccount, e.Amount, e.Fee, e.Timestamp);
            if (hasFeeDisposal)
                newState.AddFeeDisposal(e.FromAccount, e.Fee, e.Timestamp, e.RetroactiveId?.Id ?? e.CommandId?.Id);
            return newState;
        }
    }
}