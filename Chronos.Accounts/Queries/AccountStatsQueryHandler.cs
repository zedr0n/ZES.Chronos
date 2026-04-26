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
using ZES.Interfaces.Clocks;
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

        public interface IAssetPools
        {
            double RealisedGain { get; }
            double CostBasis { get; }
            
            void Acquire(Time time, double quantity, double cost);
            void Dispose(Time time, double quantity, double cost);
            void EndOfDay(Time time);
        }

        public class UkAssetPools : IAssetPools
        {
            private readonly List<Pool> _pools;
            private readonly Pool _sameDayPool = new();
            private readonly Pool _section104Pool = new();
            
            private DateTime _lastEndOfDay;
            
            public double CostBasis => _pools.Sum(p => p.Cost);
            public double RealisedGain { get; set; }

            public UkAssetPools()
            {
                _pools = [_sameDayPool, _section104Pool];
            }
            
            public void Acquire(Time time, double quantity, double cost)
            {
               _sameDayPool.Quantity += quantity;
               _sameDayPool.Cost += cost;
            }

            public void Dispose(Time time, double quantity, double cost)
            {
                var remainingQuantity = quantity;
                var remainingProceeds = cost;
                
                foreach (var pool in _pools)
                {
                    if (remainingQuantity <= 0)
                        break;
                    if (pool.Quantity <= 0)
                        continue;
                    
                    var takeQuantity = Math.Min(pool.Quantity, remainingQuantity);
                    var ratio = takeQuantity / quantity;

                    // compute the gains 
                    var proceeds = cost * ratio;
                    RealisedGain += proceeds - pool.AverageCost * takeQuantity;
                    remainingProceeds -= proceeds;
                    
                    // adjust the pool 
                    pool.Cost -= pool.AverageCost * takeQuantity;
                    pool.Quantity -= takeQuantity;
                    remainingQuantity -= takeQuantity;
                }
            }

            public void EndOfDay(Time time)
            {
                var date = time.ToDateTime().Date;
                if (date == _lastEndOfDay) 
                    return;
                
                // transfer the same-day pool to the section 104 pool
                _section104Pool.Quantity += _sameDayPool.Quantity;
                _section104Pool.Cost += _sameDayPool.Cost;
                _sameDayPool.Quantity = 0.0;
                _sameDayPool.Cost = 0.0;
                _lastEndOfDay = date;
            }
        }
        
        private record Pool
        {
            public double Quantity { get; set; }
            public double Cost { get; set; }
            public double AverageCost => Cost / Quantity; 
        }
        
        private async Task<(Dictionary<Asset, Quantity> costBasis, Dictionary<Asset, Quantity> realisedGains)> ComputeGains(AccountStatsState state, AccountStatsQuery query)
        {
            var denominator = query.Denominator;
            
            var poolsDictionary = new Dictionary<Asset, IAssetPools>();
            var costBasisDictionary = new Dictionary<Asset, Quantity>();
            var realisedGainsDictionary = new Dictionary<Asset, Quantity>();
            
            // we now need to sort all the costs by timestamp so that asset-asset swaps can be handled correctly
            foreach (var (t, costs) in state.Costs.OrderBy(x => x.Key))
            {
                // all assets involved in the transactions on t
                var assets = costs.Select(c => c.assetQuantity.Denominator).Where(a => a.AssetType != AssetType.Currency)
                    .Union(costs.Select(c => c.costQuantity.Denominator).Where(a => a.AssetType != AssetType.Currency) ).ToList();
                foreach (var asset in assets)
                {
                    var pools = poolsDictionary.GetValueOrDefault(asset, new UkAssetPools());
                    pools.EndOfDay(t);
                    
                    // process all purchases first 
                    foreach (var (q,c) in costs.Where(x => (x.assetQuantity.Denominator == asset && x.assetQuantity.Amount > 0) || (x.costQuantity.Denominator == asset && x.costQuantity.Amount < 0)) )
                    {
                        var quote = 1.0;
                        if(c.Denominator != denominator)
                        {
                            var assetQuote = await _handler.Handle(new AssetQuoteQuery(c.Denominator, denominator)
                            {
                                Timestamp = t,
                                UpdateQuote = query.QueryNet,
                            });
                            if (assetQuote != null)
                                quote = assetQuote.Quantity.Amount;
                            else
                                throw new InvalidOperationException($"No quote for asset {AssetPair.Fordom(c.Denominator, denominator)} at {query.Timestamp}");
                        }
                        
                        if (q.Denominator == asset)
                            pools.Acquire(t, q.Amount, c.Amount * quote);
                        else
                            pools.Acquire(t, -c.Amount, -c.Amount * quote);
                    } 
                    
                    // process all disposals
                    foreach (var (q, c) in costs.Where(x =>
                                 (x.assetQuantity.Denominator == asset && x.assetQuantity.Amount < 0) ||
                                 (x.costQuantity.Denominator == asset && x.costQuantity.Amount > 0)))
                    {
                        var quote = 1.0;
                        if(c.Denominator != denominator)
                        {
                            var assetQuote = await _handler.Handle(new AssetQuoteQuery(c.Denominator, denominator)
                            {
                                Timestamp = t,
                                UpdateQuote = query.QueryNet,
                            });
                            if (assetQuote != null)
                                quote = assetQuote.Quantity.Amount;
                            else
                                throw new InvalidOperationException($"No quote for asset {AssetPair.Fordom(c.Denominator, denominator)} at {query.Timestamp}");
                        }
                        
                        if(q.Denominator == asset)
                            pools.Dispose(t, -q.Amount, -c.Amount * quote);
                        else
                            pools.Dispose(t, c.Amount, c.Amount * quote);
                    }
                    
                    poolsDictionary[asset] = pools;
                    costBasisDictionary[asset] = new Quantity(pools.CostBasis, denominator);
                    realisedGainsDictionary[asset] = new Quantity(pools.RealisedGain, denominator);
                }
            }
            
            return (costBasisDictionary, realisedGainsDictionary);
        }
    }
}