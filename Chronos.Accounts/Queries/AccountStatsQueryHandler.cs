using System;
using System.Linq;
using System.Threading.Tasks;
using Chronos.Core;
using Chronos.Core.Queries;
using ZES.Infrastructure;
using ZES.Infrastructure.Domain;
using ZES.Interfaces.Branching;
using ZES.Interfaces.Domain;

namespace Chronos.Accounts.Queries
{
    /// <inheritdoc />
    [Transient]
    public class AccountStatsQueryHandler : DefaultSingleQueryHandler<AccountStatsQuery, AccountStats, AccountStatsState>
    {
        private readonly IQueryHandler<AssetQuoteQuery, AssetQuote> _handler;
        private readonly IQueryHandler<TransactionInfoQuery, TransactionInfo> _transactionInfoHandler;

        /// <summary>
        /// Initializes a new instance of the <see cref="AccountStatsQueryHandler"/> class.
        /// </summary>
        /// <param name="manager">Projection manager</param>
        /// <param name="activeTimeline">Active timeline</param>
        /// <param name="handler">Asset price handler</param>
        /// <param name="transactionInfoHandler">Transaction info handler</param>
        public AccountStatsQueryHandler(IProjectionManager manager, ITimeline activeTimeline, IQueryHandler<AssetQuoteQuery, AssetQuote> handler, IQueryHandler<TransactionInfoQuery, TransactionInfo> transactionInfoHandler) 
            : base(manager, activeTimeline)
        {
            _handler = handler;
            _transactionInfoHandler = transactionInfoHandler;
        }

        /// <inheritdoc/>
        protected override async Task<AccountStats> Handle(IProjection<AccountStatsState> projection, AccountStatsQuery query)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection), $"{typeof(IProjection<AccountStatsState>).Name}");
            var state = projection.State;
            var total = 0.0;
            var denominator = query.Denominator;
            if (query.Denominator == null)
                denominator = state.Assets.SingleOrDefault();
            
            foreach (var (asset, amount) in state.Assets.Zip(state.Quantities, (asset, value) => (asset, value)))
            {
                var price = 1.0;
                if (asset.AssetId != denominator?.AssetId)
                {
                    var queryResult = await _handler.Handle(new AssetQuoteQuery(asset, denominator)
                    {
                        Timestamp = query.Timestamp,
                        QueryNet = query.QueryNet
                    });
                    if (queryResult != null)
                        price = queryResult.Quantity.Amount;
                    else
                        return null;
                }

                total += amount * price;
            }

            foreach (var txId in state.Transactions)
            {
                var tx = await _transactionInfoHandler.Handle(new TransactionInfoQuery(txId, denominator)
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

            return new AccountStats(new Quantity(total, denominator));
        }
    }
}