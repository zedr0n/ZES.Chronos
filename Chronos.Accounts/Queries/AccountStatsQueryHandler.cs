using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Chronos.Core;
using Chronos.Core.Queries;
using ZES.Infrastructure;
using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Accounts.Queries
{
    [Transient]
    public class AccountStatsQueryHandler : DefaultSingleQueryHandler<AccountStatsQuery, AccountStats, AccountStatsState>
    {
        private readonly IQueryHandler<AssetPriceQuery, AssetPrice> _handler;
        private readonly IQueryHandler<TransactionInfoQuery, TransactionInfo> _transactionInfoHandler;
        
        public AccountStatsQueryHandler(IProjectionManager manager, IQueryHandler<AssetPriceQuery, AssetPrice> handler, IQueryHandler<TransactionInfoQuery, TransactionInfo> transactionInfoHandler) 
            : base(manager)
        {
            _handler = handler;
            _transactionInfoHandler = transactionInfoHandler;
        }

        protected override async Task<AccountStats> Handle(IProjection<AccountStatsState> projection, AccountStatsQuery query)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection), $"{typeof(IProjection<AccountStatsState>).Name}");
            var state = projection.State;
            var total = 0.0;
            foreach (var (asset, amount) in state.Assets.Zip(state.Quantities, (asset, value) => (asset, value)))
            {
                var price = 1.0;
                if (asset != query.Denominator)
                    price = (await _handler.Handle(new AssetPriceQuery(asset, query.Denominator) { Timestamp = query.Timestamp })).Price;

                total += amount * price;
            }

            foreach (var txId in state.Transactions)
            {
                var tx = await _transactionInfoHandler.Handle(new TransactionInfoQuery(txId, query.Denominator)
                {
                    ConvertToDenominatorAtTxDate = query.ConvertToDenominatorAtTxDate,
                    Timestamp = query.Timestamp,
                });
                if (tx == default)
                    return default;
                
                var amount = tx.Quantity.Amount;
                switch (tx.TransactionType)
                {
                   case Transaction.TransactionType.Sell:
                   case Transaction.TransactionType.Spend:
                       amount *= -1.0;
                       break;
                   default:
                       break;
                }
                
                total += amount;
            }

            return new AccountStats(new Quantity(total, query.Denominator));
        }
    }
}