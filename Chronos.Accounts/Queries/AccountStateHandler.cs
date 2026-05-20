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
            state.AccountName = e.Name;
            return state;
        }
        
        public AccountState Handle (AssetDeposited e, AccountState state)
        {
            state.Add(e.Quantity, e.Timestamp);
            return state;
        }

        public AccountState Handle(TransactionAdded e, AccountState state)
        {
            state.Transactions.Add(e.TxId);
            return state;
        }

        public AccountState Handle(StockSplitAdded e, AccountState state)
        {
            state.AddSplit(e.ForAsset, e.Timestamp, e.Ratio);
            return state;
        }

        public AccountState Handle(AssetTransactionStarted e, AccountState state)
        {
            state.AddCost(e.Asset, e.Cost, e.Fee, e.Timestamp, e.RetroactiveId?.Id ?? e.CommandId?.Id);
            if (e.AssetTransactionType == AssetTransactionType.Income && e.Cost.Denominator.AssetType == AssetType.Currency)
                state.AddIncome(e.Cost, e.Timestamp);
            if (e.AssetTransactionType == AssetTransactionType.Spend)
            {
                if(e.Cost.Denominator.AssetType != AssetType.Currency)
                    throw new InvalidOperationException("Spend transactions must be denominated in currency assets");
                
                state.AddSpend(e.Cost, e.Timestamp);
            }

            return state;
        }

        public AccountState Handle(QuoteAdded e, AccountState state)
        {
            state.AddQuote(e.AggregateRootId(), e.Close, e.Timestamp);
            return state;
        }

        public AccountState Handle(TransferStarted e, AccountState state)
        {
            var hasFeeDisposal = e.Fee != null && e.Fee.IsValid() && e.Fee.Amount != 0 && e.Fee.Denominator.AssetType != AssetType.Currency;

            if (e.Amount.Denominator.AssetType == AssetType.Currency && !hasFeeDisposal)
                return state;

            if (e.Amount.Denominator.AssetType != AssetType.Currency)
                state.AddAssetTransfer(e.FromAccount, e.ToAccount, e.Amount, e.Fee, e.Timestamp);
            if (hasFeeDisposal)
                state.AddFeeDisposal(e.FromAccount, e.Fee, e.Timestamp, e.RetroactiveId?.Id ?? e.CommandId?.Id);
            return state;
        }
    }
}
