using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Chronos.Core;
using Chronos.Core.Queries;
using NodaTime;
using ZES.Infrastructure;
using ZES.Infrastructure.Domain;
using ZES.Interfaces.Branching;
using ZES.Interfaces.Domain;
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
        private readonly ITimeline _timeline;
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
            _timeline = activeTimeline;
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
            public Quantity RealisedGain { get; set; }
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
            
            var (costBasis, realisedGains) = await ComputeGains(state, query);
            var assets = new HashSet<Asset>();

            foreach (var (asset, amount) in state.Assets.Zip(state.Quantities, (asset, value) => (asset, value)))
            {
                var price = 1.0;
                assets.Add(asset);
                
                // don't include the zero positions
                //if (amount == 0)
                //    continue;
                
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
                positionData.CostBasis = costBasis.GetValueOrDefault(asset, new Quantity(0, denominator));
                positionData.RealisedGain = realisedGains.GetValueOrDefault(asset, new Quantity(0, denominator)); 
                total += amount * price;
            }
            

            // compute IRR
            var extCashflows = new List<(Instant time, double amount)>();
            
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

                if (tx.TransactionType == Transaction.TransactionType.Transfer)
                    extCashflows.Add((tx.Date, -tx.Quantity.Amount));
                
                total += tx.Quantity.Amount;
                if(tx.TransactionType == Transaction.TransactionType.Dividend)
                    totalDividend += tx.Quantity.Amount;
                
                if (tx.TransactionType != Transaction.TransactionType.Dividend || tx.AssetId == null) continue;

                var asset = assets.SingleOrDefault(a => a.AssetId == tx.AssetId);
                if (asset == null)
                    throw new InvalidOperationException($"Asset {tx.AssetId} not found in positions");
                
                if (positions.TryGetValue(tx.AssetId, out var positionData))
                    positionData.Dividend = tx.Quantity with { Amount = positionData.Dividend.Amount + tx.Quantity.Amount };
                else
                {
                    positions[tx.AssetId] = new PositionData
                    {
                        Position = new Quantity(0, asset),
                        Value = new Quantity(0, denominator),
                        Dividend = tx.Quantity,
                        CostBasis = new Quantity(0, denominator),
                        RealisedGain = realisedGains.GetValueOrDefault(asset, new Quantity(0, denominator)), 
                    };
                }
            }
            var now = query.Timestamp?.ToInstant() ?? _timeline.Now.ToInstant();
            extCashflows.Add((now, total));
            var irr = query.ComputeIrr ? IrrSolver.Solve(extCashflows) : 0.0;

            return new AccountStats(new Quantity(total, denominator))
            {
                Positions = positions.Values.Select(p => p.Position).ToList(),
                Values = positions.Values.Select(p => p.Value).ToList(),
                Dividends = positions.Values.Select(p => p.Dividend).ToList(),
                CostBasis = positions.Values.Select(p => p.CostBasis).ToList(),
                CashBalance = new Quantity(total - positions.Values.Sum(v => v.Value.Amount), denominator),
                RealisedGains = positions.Values.Select(p => p.RealisedGain).ToList(),
                TotalDividend = new Quantity(totalDividend, denominator),
                ExternalCashflows = extCashflows.Take(extCashflows.Count - 1).Select( x => (x.time, new Quantity(x.amount, denominator))).ToList(),
                Irr = irr
            };
        }
        
        private async Task<(Dictionary<Asset, Quantity> costBasis, Dictionary<Asset, Quantity> realisedGains)> ComputeGains(AccountStatsState state, AccountStatsQuery query)
        {
            var denominator = query.Denominator;
            
            var costBasisDictionary = new Dictionary<Asset, Quantity>();
            var realisedGainsDictionary = new Dictionary<Asset, Quantity>();
            
            foreach (var (asset, costs) in state.Costs)
            {
                var costBasis = 0.0;
                var realisedGain = 0.0;
                foreach (var (q, c, t) in costs.OrderBy(x => x.timestamp))
                {
                    var positions = state.GetPositions(t);
                    
                    var quote = 1.0;
                    if(c.Denominator != denominator)
                    {
                        var assetQuote = await _handler.Handle(new AssetQuoteQuery(c.Denominator, denominator)
                        {
                            Timestamp = t,
                            UpdateQuote = query.QueryNet
                        });
                        if (assetQuote != null)
                            quote = assetQuote.Quantity.Amount;
                        else
                            throw new InvalidOperationException($"No quote for asset {AssetPair.Fordom(c.Denominator, denominator)} at {query.Timestamp}");
                    }
                    
                    switch (q.Amount)
                    {
                        // purchase
                        case >= 0:
                            // the cost can be in different asset
                            costBasis += c.Amount * quote;
                            break;
                        // sale
                        case < 0:
                        {
                            if (positions.Value.TryGetValue(asset, out var position))
                            {
                                // we need to subtract the amount from the position to compute the cost basis
                                position -= q.Amount;
                                var costBasisPrice = costBasis / position;
                                realisedGain += -c.Amount*quote + costBasisPrice * q.Amount;
                                costBasis += costBasisPrice * q.Amount;
                            }
                            else
                                throw new InvalidOperationException($"Position for asset {asset} not found at timestamp {t}");
                            
                            break;
                        }
                    }
                    
                    costBasisDictionary[asset] = new Quantity(costBasis, denominator);
                    realisedGainsDictionary[asset] = new Quantity(realisedGain, denominator);

                    if (c.Denominator.AssetType == AssetType.Currency) 
                        continue;

                    // if we are exchanging asset to asset, we need to adjust the cost basis and realised gains of the opposing asset as well
                    var opposingCostBasis = costBasisDictionary.GetValueOrDefault(c.Denominator, new Quantity(0, denominator)).Amount;
                    var opposingRealisedGain = realisedGainsDictionary.GetValueOrDefault(c.Denominator, new Quantity(0, denominator)).Amount;
                    switch (c.Amount)
                    {
                        // purchase of the opposing asset
                        case < 0:
                            opposingCostBasis += -c.Amount * quote;
                            break;
                        // sale of the opposing asset
                        case >= 0:
                            if (positions.Value.TryGetValue(c.Denominator, out var costPosition))
                            {
                                costPosition += c.Amount;
                                var opposingCostPrice = opposingCostBasis / costPosition;
                               
                                opposingRealisedGain += c.Amount * quote - opposingCostPrice * c.Amount;
                                opposingCostBasis += -opposingCostPrice * c.Amount; 
                            }
                            else
                                throw new InvalidOperationException($"Position for asset {c.Denominator} not found at timestamp {t}");
                            
                            break;
                    }
                    
                    costBasisDictionary[c.Denominator] = new Quantity(opposingCostBasis, denominator);
                    realisedGainsDictionary[c.Denominator] = new Quantity(opposingRealisedGain, denominator);
                }
            }
            
            return (costBasisDictionary, realisedGainsDictionary);
        }
    }
}