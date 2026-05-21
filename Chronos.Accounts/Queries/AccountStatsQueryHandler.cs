using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Chronos.Core;
using Chronos.Core.Queries;
using NodaTime;
using ZES.Infrastructure;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.Utils;
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
    public class AccountStatsQueryHandler : DefaultQueryHandler<AccountStatsQuery, AccountStats, NullState>
    {
        private readonly ITimeline _timeline;
        private readonly IQueryHandler<CombinedAccountStateQuery, AccountState> _accountStateHandler;
        private readonly IQueryHandler<AssetQuoteQuery, AssetQuote> _handler;
        private readonly IQueryHandler<TransactionInfoQuery, TransactionInfo> _transactionInfoHandler;
        private readonly IQueryHandler<AssetLedgerQuery, AssetLedger> _ledgerHandler;
        private readonly IQueryHandler<CapitalGainsQuery, CapitalGains> _capitalGainsHandler;
        
        private readonly ILog _log;
        private readonly AssetPoolFactory _assetPoolFactory;

        /// <summary>
        /// Handles the <see cref="AccountStatsQuery"/> by retrieving account-related data
        /// and computing statistics such as positions, account values, and cash balances.
        /// </summary>
        /// <remarks>
        /// This implementation processes the query by interacting with projection states and related handlers.
        /// </remarks>
        public AccountStatsQueryHandler(IProjectionManager manager, ITimeline activeTimeline,
            IQueryHandler<CombinedAccountStateQuery, AccountState> accountStateHandler,
            IQueryHandler<AssetQuoteQuery, AssetQuote> handler,
            IQueryHandler<TransactionInfoQuery, TransactionInfo> transactionInfoHandler,
            IQueryHandler<AssetLedgerQuery, AssetLedger> ledgerHandler,
            ILog log,
            AssetPoolFactory assetPoolFactory, IQueryHandler<CapitalGainsQuery, CapitalGains> capitalGainsHandler)
            : base(manager, activeTimeline)
        {
            _timeline = activeTimeline;
            _accountStateHandler = accountStateHandler;
            _handler = handler;
            _transactionInfoHandler = transactionInfoHandler;
            _ledgerHandler = ledgerHandler;
            _log = log;
            _assetPoolFactory = assetPoolFactory;
            _capitalGainsHandler = capitalGainsHandler;
        }

        protected override async Task<AccountStats> Handle(AccountStatsQuery query)
        {
            Predicate = s => false;
            return await base.Handle(query);
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
        protected override async Task<AccountStats> Handle(IProjectionState<NullState> projection, AccountStatsQuery query)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection), $"{typeof(IProjection<AccountState>).Name}");
            var timestamp = query.Timestamp;
            var state = await _accountStateHandler.Handle(new CombinedAccountStateQuery([query.Name])
            {
                Timeline = query.Timeline,
                Timestamp = timestamp,
                AdditionalTimestamps = query.AdditionalTimestamps
            });
            
            var stats = await Handle(state, query);
            try
            {
                var assets = query.AssetQuotes.Keys.Select(k => k.asset).Distinct().ToList();
                var denominator = query.Denominator;
                if (query.Denominator == null)
                    denominator = state.Assets.SingleOrDefault();

                foreach (var asset in assets)
                {
                    var quotes = await _handler.Handle(new AssetQuoteQuery(asset, denominator)
                    {
                        Timeline = query.Timeline,
                        Timestamp = timestamp,
                        EnforceCache = query.EnforceCache,
                        UpdateQuote = query.QueryNet,
                        AdditionalTimestamps = query.AdditionalTimestamps,
                        AssetQuoteOverrides = query.AssetQuoteOverrides
                    });
                    foreach (var (t, quote) in quotes.HistoricalResults)
                    {
                        query.AssetQuotes[(asset, denominator, t)] = quote;
                    }
                }
                
                foreach (var t in query.AdditionalTimestamps ?? [])
                {
                    query.Timestamp = t;
                    stats.HistoricalResults[t] = await Handle(state.HistoricalResults[t], query);
                }
            }
            finally
            {
                query.Timestamp = timestamp;
            }

            return stats;
        }

        public override async Task<AccountStats> Handle<TState>(TState tState, AccountStatsQuery query)
        { 
            if (tState is not AccountState state)
                throw new ArgumentException($"{nameof(AccountState)} expected", nameof(tState));

            var transactionInfos = query.TransactionInfos;
            
            var total = 0.0;
            var denominator = query.Denominator;
            if (query.Denominator == null)
                denominator = state.Assets.SingleOrDefault();

            var positions = new Dictionary<string, PositionData>();
            var totalDividend = 0.0;
            
            var pools = new Dictionary<Asset, IAssetPools>();
            var costBasis = new Dictionary<Asset, Quantity>();
            var realisedGains = new Dictionary<Asset, Quantity>();
            var realisedGainsPerTaxYear = new Dictionary<Asset, Dictionary<int, Quantity>>();
            var disposalGainItems = new Dictionary<Asset, List<DisposalGainItem>>();
                
            if (query.ComputeCapitalGains)
            {
                var capitalGains = await _capitalGainsHandler.Handle(state,
                    new CapitalGainsQuery(state.GetAccountNames().ToList(), denominator)
                    {
                        Timeline = query.Timeline,
                        Timestamp = query.Timestamp,
                        QueryNet = query.QueryNet,
                        EnforceCache = query.EnforceCache,
                        NumberOfMatchingDays = query.NumberOfMatchingDays,
                        AssetQuoteOverrides = query.AssetQuoteOverrides,
                        TrackDisposalLots = query.TrackDisposalLots
                    });
                costBasis = capitalGains.CostBasis;
                realisedGains = capitalGains.RealisedGains;
                pools = capitalGains.AssetPools;
                realisedGainsPerTaxYear = capitalGains.RealisedGainsPerTaxYear;
                disposalGainItems = capitalGains.DisposalGainItems;
            }
            
            var assets = new HashSet<Asset>();

            foreach (var (asset, amount) in state.Assets.Zip(state.Quantities, (asset, value) => (asset, value)))
            {
                var price = 1.0;
                assets.Add(asset);
                
                if (asset.AssetId != denominator?.AssetId && amount != 0)
                {
                    var assetQuote = await GetAssetQuote(asset, denominator, query, query.Timestamp);
                    if (assetQuote != null)
                        price = assetQuote.Quantity.Amount;
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
                if (!transactionInfos.TryGetValue((txId, denominator.AssetId), out var tx))
                {
                    tx = await _transactionInfoHandler.Handle(new TransactionInfoQuery(txId, denominator)
                    {
                        ConvertToDenominatorAtTxDate = query.ConvertToDenominatorAtTxDate,
                        QueryNet = query.QueryNet,
                        EnforceCache = query.EnforceCache,
                        //Timestamp = query.Timestamp,
                    });
                    if (tx == null)
                    {
                        _log.Error($"Transaction {txId} not found");
                        return null;
                    }
                    transactionInfos[(txId, denominator.AssetId)] = tx;
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

            foreach (var (timestamp, quantities) in state.Spend)
            {
                foreach (var q in quantities)
                {
                    var quote = 1.0;
                    if (q.Denominator != denominator)
                    {
                        var assetQuote = await GetAssetQuote(q.Denominator, denominator, query, timestamp);
                        if (assetQuote != null)
                            quote = assetQuote.Quantity.Amount;
                        else
                            throw new InvalidOperationException($"No quote for asset {AssetPair.Fordom(q.Denominator, denominator)} at {timestamp}");
                    }
                    extCashflows.Add((timestamp.ToInstant(), -q.Amount * quote));
                }
            }
            
            foreach (var (timestamp, transfers) in state.AssetTransfers)
            {
                foreach (var transfer in transfers)
                {
                    var quote = 1.0;
                    if (transfer.Denominator != denominator)
                    {
                        var assetQuote = await GetAssetQuote(transfer.Denominator, denominator, query, timestamp);
                        if (assetQuote != null)
                            quote = assetQuote.Quantity.Amount;
                        else
                            throw new InvalidOperationException($"No quote for asset {AssetPair.Fordom(transfer.Denominator, denominator)} at {timestamp}");
                    }
                    extCashflows.Add((timestamp.ToInstant(), -transfer.Amount * quote));
                }
            }
            
            var now = query.Timestamp?.ToInstant() ?? _timeline.Now.ToInstant();
            extCashflows.Add((now, total));
            var irr = query.ComputeIrr ? IrrSolver.Solve(extCashflows) : 0.0;

            var income = 0.0;
            foreach(var (t, lst) in state.Income)
            {
                foreach (var i in lst)
                {
                    var quote = 1.0;
                    if (i.Denominator != denominator)
                    {
                        var assetQuote = await GetAssetQuote(i.Denominator, denominator, query, t);
                        if (assetQuote != null)
                            quote = assetQuote.Quantity.Amount;
                        else
                            throw new InvalidOperationException($"No quote for asset {AssetPair.Fordom(i.Denominator, denominator)} at {t}");
                    }
                    income += i.Amount * quote;
                }
            }
            
            return new AccountStats(new Quantity(total, denominator))
            {
                Positions = positions.Values.Select(p => p.Position).ToList(),
                Values = positions.Values.Select(p => p.Value).ToList(),
                Dividends = positions.Values.Select(p => p.Dividend).ToList(),
                CostBasis = positions.Values.Select(p => p.CostBasis).ToList(),
                CashBalance = new Quantity(total - positions.Values.Sum(v => v.Value.Amount), denominator),
                RealisedGains = positions.Values.Select(p => p.RealisedGain).ToList(),
                RealisedGainsPerTaxYear = realisedGainsPerTaxYear,
                DisposalGainItems = disposalGainItems,
                TotalDividend = new Quantity(totalDividend, denominator),
                ExternalCashflows = extCashflows.Take(extCashflows.Count - 1).Select( x => (x.time, new Quantity(x.amount, denominator))).ToList(),
                Irr = irr,
                AssetPools = pools,
                State = state,
                Income = new Quantity(income, denominator)
            };
        }
        
        private async Task<AssetQuote> GetAssetQuote(Asset asset, Asset denominator, AccountStatsQuery query, Time timestamp)
        {
            var key = (asset, denominator, timestamp);

            query.AssetQuotes ??= new Dictionary<(Asset asset, Asset denominator, Time timestamp), AssetQuote>();

            if (query.AssetQuotes.TryGetValue(key, out var cached))
                return cached;

            var quote = await _handler.Handle(new AssetQuoteQuery(asset, denominator)
            {
                Timeline = query.Timeline,
                Timestamp = timestamp,
                EnforceCache = query.EnforceCache,
                UpdateQuote = query.QueryNet,
                AssetQuoteOverrides = query.AssetQuoteOverrides
            });

            if(quote != null)
                query.AssetQuotes[key] = quote;
            return quote;
        }
    }
}