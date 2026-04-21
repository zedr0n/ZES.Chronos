using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Chronos.Core;
using Chronos.Core.Queries;
using ZES.Infrastructure;
using ZES.Infrastructure.Domain;
using ZES.Interfaces.Branching;
using ZES.Interfaces.Domain;
using ZES.Interfaces.EventStore;
using ZES.Interfaces.Infrastructure;

namespace Chronos.Accounts.Queries
{
    /// <summary>
    /// Handles the <see cref="AccountStatsQuery"/> by processing the account data and computing statistics such as positions, values, and cash balance.
    /// </summary>
    /// <remarks>
    /// This handler retrieves and transforms account-related data based on the query's specifications and projection state.
    /// </remarks>
    [Transient]
    public class AccountStatsQueryHandler : DefaultQueryHandler<AccountStatsQuery, AccountStats, AccountStatsState>
    {
        private readonly IQueryHandler<AssetQuoteQuery, AssetQuote> _handler;
        private readonly IQueryHandler<TransactionInfoQuery, TransactionInfo> _transactionInfoHandler;
        private readonly ILog _log;

        /// <summary>
        /// Handles the <see cref="AccountStatsQuery"/> by retrieving account-related data
        /// and computing statistics such as positions, account values, and cash balances.
        /// </summary>
        /// <remarks>
        /// This implementation processes the query by interacting with projection states and related handlers.
        /// </remarks>
        public AccountStatsQueryHandler(IProjectionManager manager, ITimeline activeTimeline,
            IQueryHandler<AssetQuoteQuery, AssetQuote> handler,
            IQueryHandler<TransactionInfoQuery, TransactionInfo> transactionInfoHandler, ILog log)
            : base(manager, activeTimeline)
        {
            _handler = handler;
            _transactionInfoHandler = transactionInfoHandler;
            _log = log;
        }

        protected override async Task<AccountStats> Handle(AccountStatsQuery query)
        {
            Predicate = s => (s.Type == nameof(Account) && s.SameId(query.Name)) || s.Type == nameof(AssetPair);
            return await base.Handle(query, query.Name);
        }

        private class PositionData
        {
            public Quantity Position { get; set; }
            public Quantity Value { get; set; }
            public Quantity Dividend { get; set; }
            public Quantity CostBasis { get; set; }
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

            var positions = new Dictionary<string, PositionData>();
            var totalDividend = 0.0;
            //var positions = new List<Quantity>(); 
            //var values = new List<Quantity>();
            //var dividends = new List<Quantity>();

            if (query.WithPositions)
            {
                foreach (var (asset, amount) in state.Assets.Zip(state.Quantities, (asset, value) => (asset, value)))
                {
                    var price = 1.0;
                    if (asset.AssetId != denominator?.AssetId)
                    {
                        var queryResult = await _handler.Handle(new AssetQuoteQuery(asset, denominator)
                        {
                            Timestamp = query.Timestamp,
                            UpdateQuote = query.QueryNet
                        });
                        if (queryResult != null)
                            price = queryResult.Quantity.Amount;
                        else
                            return null;
                    }

                    if (asset.AssetId == null)
                        throw new InvalidOperationException("Asset id is required");
                    
                    if(!positions.ContainsKey(asset.AssetId))
                        positions[asset.AssetId] = new PositionData();

                    var positionData = positions[asset.AssetId];
                    positionData.Dividend = new Quantity(0, denominator);
                    positionData.Position = new Quantity(amount, asset);
                    positionData.Value = new Quantity(amount * price, denominator);
                    positionData.CostBasis = new Quantity(state.CostBasis[asset], denominator);
                    total += amount * price;
                }
            }

            foreach (var txId in state.Transactions)
            {
                var tx = await _transactionInfoHandler.Handle(new TransactionInfoQuery(txId, denominator)
                {
                    ConvertToDenominatorAtTxDate = query.ConvertToDenominatorAtTxDate,
                    //Timestamp = query.Timestamp,
                });
                if (tx == null)
                {
                    _log.Error($"Transaction {txId} not found");
                    return null;
                }

                total += tx.Quantity.Amount;
                if(tx.TransactionType == Transaction.TransactionType.Dividend)
                    totalDividend += tx.Quantity.Amount;
                
                if (!query.WithPositions || tx.TransactionType != Transaction.TransactionType.Dividend ||
                    tx.AssetId == null) continue;

                if (positions.TryGetValue(tx.AssetId, out var positionData))
                    positionData.Dividend = tx.Quantity with { Amount = positionData.Dividend.Amount + tx.Quantity.Amount };
                /*else
                {
                    positions[tx.AssetId] = new PositionData
                    {
                        Position = new Quantity(0, new Asset(tx.AssetId, AssetType.Equity)),
                        Value = new Quantity(0, denominator),
                        Dividend = tx.Quantity
                    };
                }*/
            }

            return new AccountStats(new Quantity(total, denominator))
            {
                Positions = positions.Values.Select(p => p.Position).ToList(),
                Values = positions.Values.Select(p => p.Value).ToList(),
                Dividends = positions.Values.Select(p => p.Dividend).ToList(),
                CostBasis = positions.Values.Select(p => p.CostBasis).ToList(),
                CashBalance = new Quantity(total - positions.Values.Sum(v => v.Value.Amount), denominator),
                TotalDividend = new Quantity(totalDividend, denominator)
            };
        }
    }
}