using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Chronos.Core;
using Chronos.Core.Queries;
using ZES.Infrastructure;
using ZES.Infrastructure.Domain;
using ZES.Interfaces.Branching;
using ZES.Interfaces.Clocks;
using ZES.Interfaces.Domain;
using ZES.Interfaces.Infrastructure;

namespace Chronos.Accounts.Queries;

[Transient]
public class CapitalGainsQueryHandler(
    IProjectionManager manager,
    ITimeline activeTimeline,
    IQueryHandler<AssetQuoteQuery, AssetQuote> assetQuoteHandler,
    IQueryHandler<CombinedAccountStateQuery, AccountState> accountStateHandler,
    IQueryHandler<AssetLedgerQuery, AssetLedger> ledgerHandler,
    ILog log,
    AssetPoolFactory assetPoolFactory)
    : DefaultQueryHandler<CapitalGainsQuery, CapitalGains, NullState>(manager, activeTimeline)
{
    /// <inheritdoc/>
    protected override async Task<CapitalGains> Handle(IProjectionState<NullState> projection, CapitalGainsQuery query)
    {
        if (projection == null)
            throw new ArgumentNullException(nameof(projection), $"{typeof(IProjection<AccountState>).Name}");
        var timestamp = query.Timestamp;
        var initialTime = query.InitialTime;
        var initial = query.Initial;
        
        var state = await accountStateHandler.Handle(new CombinedAccountStateQuery(query.Accounts)
        {
            Timeline = query.Timeline,
            Timestamp = timestamp,
            AdditionalTimestamps = query.AdditionalTimestamps
        });
        
        if(query.AdditionalTimestamps == null)
            return await Handle(state, query);

        CapitalGains capitalGains = null;
        try
        {
            var states = new Dictionary<Time, AccountState>();
            var capitalGainsDictionary = new Dictionary<Time, CapitalGains>();
            foreach (var t in query.AdditionalTimestamps)
                states[t] = state.HistoricalResults[t];
            states[timestamp] = state;
            
            if(initialTime != null && initialTime > states.Min(x => x.Key))
                throw new InvalidOperationException($"Initial time {initialTime} is before the earliest timestamp {states.Min(x => x.Key)}");
            if (timestamp != null && states.Max(x => x.Key) > timestamp)
                throw new InvalidOperationException($"Timestamp {timestamp} is after the latest requested timestamp {states.Max(x => x.Key)}");
                
            foreach (var (t, s) in states.OrderBy(x => x.Key))
            {
                query.Timestamp = t;
                capitalGains = await Handle(s, query);
                capitalGainsDictionary[t] = capitalGains.Copy();

                query.Initial = capitalGains;
                query.InitialTime = t;
            }
            
            foreach(var t in query.AdditionalTimestamps)
                capitalGains?.HistoricalResults[t] = capitalGainsDictionary[t];
        }
        finally
        {
            query.Timestamp = timestamp;
            query.Initial = initial;
            query.InitialTime = initialTime;
        }

        return capitalGains;
    }

    public override async Task<CapitalGains> Handle<TState>(TState tState, CapitalGainsQuery query)
    {
        if (tState is not AccountState state)
            throw new ArgumentException($"{nameof(AccountState)} expected", nameof(tState));

        query.AccountStateCache ??= new Dictionary<(string accountsKey, Time timestamp), AccountState>();
        query.CapitalGainsCache ??= new Dictionary<(string accountsKey, Time timestamp, string assetId), CapitalGains>();

        var requestedAssets = query.Assets?.Where(a => a != null).ToHashSet() ?? [];
        var filterAssets = requestedAssets is { Count: > 0 };
        
        var denominator = query.Denominator;
            
        var poolsDictionary = new Dictionary<Asset, IAssetPools>();
        var costBasisDictionary = new Dictionary<Asset, Quantity>();
        var realisedGainsDictionary = new Dictionary<Asset, Quantity>();
        var realisedGainsPerTaxYearDictionary = new Dictionary<Asset, Dictionary<int, Quantity>>();
        var disposalGainItemsDictionary = new Dictionary<Asset, List<DisposalGainItem>>();

        var t0 = query.InitialTime ?? Time.MinValue;
        var initialCgt = query.Initial;
        
        if (t0 != Time.MinValue && initialCgt != null)
        {
            poolsDictionary = initialCgt.AssetPools.ToDictionary(x => x.Key, x => x.Value.Copy());
            costBasisDictionary = new Dictionary<Asset, Quantity>(initialCgt.CostBasis);
            realisedGainsDictionary = new Dictionary<Asset, Quantity>(initialCgt.RealisedGains);
            realisedGainsPerTaxYearDictionary = initialCgt.RealisedGainsPerTaxYear.ToDictionary(x => x.Key, x => new Dictionary<int, Quantity>(x.Value));
            disposalGainItemsDictionary = initialCgt.DisposalGainItems.ToDictionary(x => x.Key, x => x.Value.ToList());
        }
        
        var assetTransferIn = state.GetAssetTransfersIn();
        var assetTransfersOut = state.GetAssetTransfersOut();
        var feeDisposals = state.FeeDisposals;

        var accountStateDictionary = query.AccountStateCache; 

        var assetLedger = await ledgerHandler.Handle(new AssetLedgerQuery() { Timeline = query.Timeline });
        if (assetLedger == null)
            throw new InvalidOperationException($"Asset ledger not found");

        var allTransferTimestampsByAccount = new Dictionary<string, HashSet<Time>>();
        var joinedTransferTimestampsByAccount = new Dictionary<string, HashSet<Time>>();
        
        foreach (var (timestamp, transfersIn) in assetTransferIn)
        {
            foreach (var (fromAccount, quantity) in transfersIn)
            {
                AddTimestamp(allTransferTimestampsByAccount, fromAccount, timestamp);

                var hasCrossAccountMatchingPair = assetLedger.HasCrossAccountMatchingPair(
                    state.GetAccountNames(),
                    fromAccount,
                    quantity.Denominator,
                    timestamp,
                    query.NumberOfMatchingDays);

                if(hasCrossAccountMatchingPair)
                    AddTimestamp(joinedTransferTimestampsByAccount, fromAccount, timestamp);
            }
        }      
        
        foreach (var (timestamp, transfersOut) in assetTransfersOut)
        {
            foreach (var (toAccount, quantity) in transfersOut)
            {
                var hasCrossAccountMatchingPair = assetLedger.HasCrossAccountMatchingPair(
                    state.GetAccountNames(),
                    toAccount,
                    quantity.Denominator,
                    timestamp,
                    query.NumberOfMatchingDays);

                if (hasCrossAccountMatchingPair)
                {
                    AddTimestamp(allTransferTimestampsByAccount, toAccount, timestamp);
                    AddTimestamp(joinedTransferTimestampsByAccount, toAccount, timestamp);
                }
            }
        }        
        
        var allJoinedTimestamps = joinedTransferTimestampsByAccount.Values
            .SelectMany(x => x)
            .Distinct()
            .OrderBy(x => x)
            .ToList();
        
       foreach(var timestamp in allJoinedTimestamps)
          AddTimestamp(allTransferTimestampsByAccount, state.AccountName, timestamp);
        
        // load all the account states for the accounts involved in transfers-in 
        await FillStateCache(allTransferTimestampsByAccount, accountStateDictionary, query);

        var allTimestamps = state.Costs.Keys?.Union(assetTransferIn.Select(x => x.Key)).Union(assetTransfersOut.Select(x => x.Key)).Union(feeDisposals.Keys).Where(t => t > t0) ?? [];
        
        // we now need to sort all the costs by timestamp so that asset-asset swaps can be handled correctly
        const double tolerance = 1e-8;
        
        foreach (var t in allTimestamps.OrderBy(x => x))
        {
            var costs = state.GetCosts(t).ToList(); 
            var transfersIn = assetTransferIn.GetValueOrDefault(t, []);
            var transfersOut = assetTransfersOut.GetValueOrDefault(t, []);
            var assetsHandledByFullTransfer = new HashSet<Asset>();
            
            // handle transfers in
            foreach (var (fromAccount, q) in transfersIn)
            {
                if (filterAssets && !requestedAssets.Contains(q.Denominator))
                    continue;
                
                var hasCrossAccountMatchingPair = assetLedger.HasCrossAccountMatchingPair(state.GetAccountNames(), fromAccount, q.Denominator, t, query.NumberOfMatchingDays);

                var fromAccountState = accountStateDictionary[(fromAccount, t)];
                
                if (hasCrossAccountMatchingPair)
                {
                    var isFullTransfer = fromAccountState.IsFullTransfer(q.Denominator, t, q.Amount, out var total, tolerance);
                    if (!isFullTransfer)
                        log.Warn($"Partial transfer detected for {q.Denominator} from {fromAccount} to {state.AccountName} on {t} of amount {q.Amount} out of {total}");

                    var combinedAccountName = string.Join("|",state.GetAccountNames().Append(fromAccount).Distinct().OrderBy(x => x).ToList());
                    if (!accountStateDictionary.TryGetValue((combinedAccountName, t), out var combinedState))
                    {
                        var currentState = accountStateDictionary[(state.AccountName, t)]; 
                        combinedState = currentState.Copy().CombineWith(fromAccountState);
                        accountStateDictionary[(combinedAccountName, t)] = combinedState;
                    }

                    var coupledAssets = GetCoupledAssets(fromAccountState, t, q.Denominator);
                    var capitalGains = await GetCapitalGains(combinedState, t, coupledAssets, query);
                    if(!capitalGains.AssetPools.TryGetValue(q.Denominator, out var joinedPool))
                        throw new InvalidOperationException($"Asset {q.Denominator} not found in joined pool");

                    var ratio = q.Amount / total;
                    var scaledPool = joinedPool.Slice(t,ratio);
                    
                    poolsDictionary[q.Denominator] = scaledPool;
                    costBasisDictionary[q.Denominator] = new Quantity(scaledPool.CostBasis, denominator);
                    realisedGainsDictionary[q.Denominator] = new Quantity(scaledPool.RealisedGain, denominator);
                    realisedGainsPerTaxYearDictionary[q.Denominator] = scaledPool.GetRealisedGainsPerTaxYear()
                        .ToDictionary(x => x.Key, x => new Quantity(x.Value, denominator)); 
                    disposalGainItemsDictionary[q.Denominator] =
                        scaledPool.GetDisposalGains().Select(x => new DisposalGainItem(x)).ToList(); 
                    assetsHandledByFullTransfer.Add(q.Denominator);
                }
                else
                {
                    var coupledAssets = GetCoupledAssets(fromAccountState, t, q.Denominator);
                    var fromCapitalGains = await GetCapitalGains(fromAccountState, t, coupledAssets, query);
                    var fromPools = fromCapitalGains.AssetPools;
                    var pools = poolsDictionary.GetValueOrDefault(q.Denominator, assetPoolFactory.Create(query.NumberOfMatchingDays, query.TrackDisposalLots));
                    pools.TransferFrom(t, fromPools[q.Denominator].Copy(), q.Amount);
                    poolsDictionary[q.Denominator] = pools;
                }
            }
             
            // all assets involved in the transactions on t
            var assets = costs
                .Select(c => c.assetQuantity.Denominator).Where(a => a.AssetType != AssetType.Currency)
                .Union(costs.Select(c => c.costQuantity.Denominator).Where(a => a.AssetType != AssetType.Currency) )
                .Where(a => !assetsHandledByFullTransfer.Contains(a))
                .Where(a => !filterAssets || requestedAssets.Contains(a))
                .ToList();
            foreach (var asset in assets)
            {
                var pools = poolsDictionary.GetValueOrDefault(asset, assetPoolFactory.Create(query.NumberOfMatchingDays, query.TrackDisposalLots));
                
                // process all purchases first 
                foreach (var (q,c, sourceOperationId) in costs.Where(x => (x.assetQuantity.Denominator == asset && x.assetQuantity.Amount > 0) || (x.costQuantity.Denominator == asset && x.costQuantity.Amount < 0)) )
                {
                    var quote = 1.0;
                    if(c.Denominator != denominator)
                    {
                        var assetQuote = await assetQuoteHandler.Handle(new AssetQuoteQuery(c.Denominator, denominator)
                        {
                            Timestamp = t,
                            UpdateQuote = query.QueryNet,
                            EnforceCache = query.EnforceCache,
                            SourceOperationId = sourceOperationId?.ToString(),
                            AssetQuoteOverrides = query.AssetQuoteOverrides
                        });
                        if (assetQuote != null)
                            quote = assetQuote.Quantity.Amount;
                        else
                            throw new InvalidOperationException($"No quote for asset {AssetPair.Fordom(c.Denominator, denominator)} at {t}");
                    }
                    
                    if (q.Denominator == asset)
                        pools.Acquire(t, q.Amount, c.Amount * quote);
                    else
                        pools.Acquire(t, -c.Amount, -c.Amount * quote);
                } 
                
                // process all disposals
                foreach (var (q, c, sourceOperationId) in costs.Where(x =>
                             (x.assetQuantity.Denominator == asset && x.assetQuantity.Amount < 0) ||
                             (x.costQuantity.Denominator == asset && x.costQuantity.Amount > 0)))
                {
                    var quote = 1.0;
                    if(c.Denominator != denominator)
                    {
                        var assetQuote = await assetQuoteHandler.Handle(new AssetQuoteQuery(c.Denominator, denominator)
                        {
                            Timestamp = t,
                            UpdateQuote = query.QueryNet,
                            EnforceCache = query.EnforceCache,
                            SourceOperationId = sourceOperationId?.ToString(),
                            AssetQuoteOverrides = query.AssetQuoteOverrides
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
            }
            
            foreach (var (toAccount, q) in transfersOut)
            {
                if (filterAssets && !requestedAssets.Contains(q.Denominator))
                    continue;
               
                var pools = poolsDictionary.GetValueOrDefault(q.Denominator, assetPoolFactory.Create(query.NumberOfMatchingDays, query.TrackDisposalLots));
                
                // Even when excluding the transfer-out itself at the query date, advance the
                // source pools to the transfer date so transfers-in receive a normalized state.                    
                if (t == query.Timestamp && !query.IncludeTransfersOutAtQueryDate)
                {
                    pools.AdvanceTo(t);
                    poolsDictionary[q.Denominator] = pools;
                    continue;
                }                
                
                var hasCrossAccountMatchingPair = assetLedger.HasCrossAccountMatchingPair(state.GetAccountNames(), toAccount, q.Denominator, t, query.NumberOfMatchingDays);
                if (hasCrossAccountMatchingPair)
                {
                    var toAccountState = accountStateDictionary[(toAccount, t)];
                    var thisAccountState = accountStateDictionary[(state.AccountName, t)];
                                   
                    var isFullTransfer = thisAccountState.IsFullTransfer(q.Denominator, t, q.Amount, out var total, tolerance);
                    if (!isFullTransfer)
                        log.Warn($"Partial transfer detected for {q.Denominator} from {state.AccountName} to {toAccount} on {t} of amount {q.Amount} out of {total}");

                    if (!isFullTransfer)
                    {
                        var combinedAccountName = string.Join("|",state.GetAccountNames().Append(toAccount).Distinct().OrderBy(x => x).ToList());
                        if (!accountStateDictionary.TryGetValue((combinedAccountName, t), out var combinedState))
                        {
                            var currentState = accountStateDictionary[(state.AccountName, t)]; 
                            combinedState = currentState.Copy().CombineWith(toAccountState);
                            accountStateDictionary[(combinedAccountName, t)] = combinedState;
                        }

                        var coupledAssets = GetCoupledAssets(toAccountState, t, q.Denominator);
                        var capitalGains = await GetCapitalGains(combinedState, t, coupledAssets, query);
                        if(!capitalGains.AssetPools.TryGetValue(q.Denominator, out var joinedPool))
                            throw new InvalidOperationException($"Asset {q.Denominator} not found in joined pool");

                        var ratio = q.Amount / total;
                        var scaledPool = joinedPool.Slice(t, 1.0-ratio);
                    
                        poolsDictionary[q.Denominator] = scaledPool;
                        costBasisDictionary[q.Denominator] = new Quantity(scaledPool.CostBasis, denominator);
                        realisedGainsDictionary[q.Denominator] = new Quantity(scaledPool.RealisedGain, denominator);
                        realisedGainsPerTaxYearDictionary[q.Denominator] = scaledPool.GetRealisedGainsPerTaxYear()
                            .ToDictionary(x => x.Key, x => new Quantity(x.Value, denominator)); // new Dictionary<int, Quantity>(capitalGains.RealisedGainsPerTaxYear[q.Denominator]);
                        disposalGainItemsDictionary[q.Denominator] =
                            scaledPool.GetDisposalGains().Select(x => new DisposalGainItem(x)).ToList(); 
                        continue;                        
                    }
                }
                
                pools.TransferOut(t, q.Amount);
                poolsDictionary[q.Denominator] = pools;
            }

            // handle fee disposals
            foreach (var (fee, sourceOperationId) in state.GetFeeDisposals(t))
            {
                if (filterAssets && !requestedAssets.Contains(fee.Denominator))
                    continue;
                
                if (assetsHandledByFullTransfer.Contains(fee.Denominator))
                    continue;
                
                var pools = poolsDictionary.GetValueOrDefault(fee.Denominator, assetPoolFactory.Create(query.NumberOfMatchingDays, query.TrackDisposalLots));
                var assetQuote = await assetQuoteHandler.Handle(new AssetQuoteQuery(fee.Denominator, denominator)
                {
                    Timestamp = t,
                    UpdateQuote = query.QueryNet,
                    EnforceCache = query.EnforceCache,
                    SourceOperationId = sourceOperationId?.ToString(),
                    AssetQuoteOverrides = query.AssetQuoteOverrides
                });
                if (assetQuote != null)
                    pools.Dispose(t, fee.Amount, fee.Amount * assetQuote.Quantity.Amount);
                poolsDictionary[fee.Denominator] = pools;
            }
        }

        foreach (var asset in poolsDictionary.Keys)
        {
            var pools = poolsDictionary[asset];
            costBasisDictionary[asset] = new Quantity(pools.CostBasis, denominator);
            realisedGainsDictionary[asset] = new Quantity(pools.RealisedGain, denominator);
            realisedGainsPerTaxYearDictionary[asset] = pools.GetRealisedGainsPerTaxYear().ToDictionary(x => x.Key, x => new Quantity(x.Value, denominator));
            disposalGainItemsDictionary[asset] = pools.GetDisposalGains().Select(x => new DisposalGainItem(x)).ToList();
        }
        
        return new CapitalGains()
        {
            CostBasis = costBasisDictionary,
            RealisedGains = realisedGainsDictionary,
            RealisedGainsPerTaxYear = realisedGainsPerTaxYearDictionary,
            DisposalGainItems = disposalGainItemsDictionary,
            AssetPools = poolsDictionary
        };
    }

    private async Task FillStateCache(Dictionary<string, HashSet<Time>> timestampsByAccount, 
    Dictionary<(string account, Time timestamp), AccountState> cache,
    CapitalGainsQuery query)
    {
        foreach (var (account, timestampSet) in timestampsByAccount)
        {
            var timestamps = timestampSet.OrderBy(t => t).ToList();
            if (timestamps.Count == 0)
                continue;
            
            var latest = timestamps[^1];
            var extraTimestamps = timestamps.Take(timestamps.Count - 1).ToList();
            var accounts = account.Split('|').ToList();
            var state =  await accountStateHandler.Handle(new CombinedAccountStateQuery(accounts)
            {
                Timeline = query.Timeline,
                Timestamp = latest,
                AdditionalTimestamps = extraTimestamps
            });
            if (state == null)
                throw new InvalidOperationException($"Account {account} not found");

            cache[(account, latest)] = state;
            foreach(var t in extraTimestamps)
                cache[(account, t)] = state.HistoricalResults[t];
        }
    }

    private static string AssetsKey(IEnumerable<Asset> assets)
    {
        var assetList = assets?.ToList();
        if (assetList is not { Count: > 0 })
            return null;

        return string.Join("|", assetList
            .Select(a => a.AssetId ?? a.ToString())
            .OrderBy(x => x));
    }
    
    private async Task<CapitalGains> GetCapitalGains(
        AccountState state,
        Time timestamp,
        List<Asset> assets,
        CapitalGainsQuery parentQuery,
        bool includeTransfersOutAtQueryDate = false)
    {
        var accounts = state.GetAccountNames().Distinct().OrderBy(x => x).ToList();
        var assetsKey = AssetsKey(assets);
        
        // Cached CapitalGains instances are treated as immutable snapshots. Copy individual
        // pools before passing them to operations that mutate pool state.
        if (parentQuery.CapitalGainsCache.TryGetValue((state.AccountName, timestamp, assetsKey), out var cached))
            return cached;

        var previous = parentQuery.CapitalGainsCache
            .Where(x => x.Key.accountsKey == state.AccountName && x.Key.assetsKey == assetsKey && x.Key.timestamp < timestamp)
            .OrderByDescending(x => x.Key.timestamp)
            .FirstOrDefault();

        var childQuery = new CapitalGainsQuery(accounts, parentQuery.Denominator, assets)
        {
            Timeline = parentQuery.Timeline,
            Timestamp = timestamp,
            QueryNet = parentQuery.QueryNet,
            EnforceCache = parentQuery.EnforceCache,
            IncludeTransfersOutAtQueryDate = includeTransfersOutAtQueryDate,
            NumberOfMatchingDays = parentQuery.NumberOfMatchingDays,
            AssetQuoteOverrides = parentQuery.AssetQuoteOverrides,
            TrackDisposalLots = parentQuery.TrackDisposalLots,

            InitialTime = previous.Value == null ? null : previous.Key.timestamp,
            Initial = previous.Value?.Copy(),

            AccountStateCache = parentQuery.AccountStateCache,
            CapitalGainsCache = parentQuery.CapitalGainsCache
        };

        var capitalGains = await Handle(state, childQuery);
        parentQuery.CapitalGainsCache[(state.AccountName, timestamp, assetsKey)] = capitalGains;

        return capitalGains;
    }

    private static void AddTimestamp(
        Dictionary<string, HashSet<Time>> timestampsByAccount,
        string account,
        Time timestamp)
    {
        if (!timestampsByAccount.TryGetValue(account, out var timestamps))
        {
            timestamps = [];
            timestampsByAccount[account] = timestamps;
        }

        timestamps.Add(timestamp);
    }
    
    private static List<Asset> GetCoupledAssets(AccountState state, Time timestamp, Asset asset)
    {
        var assets = new HashSet<Asset> { asset };

        foreach (var (assetQuantity, costQuantity, _) in state.GetCosts(timestamp))
        {
            if (assetQuantity.Denominator == asset || costQuantity.Denominator == asset)
            {
                if (assetQuantity.Denominator.AssetType != AssetType.Currency)
                    assets.Add(assetQuantity.Denominator);

                if (costQuantity.Denominator.AssetType != AssetType.Currency)
                    assets.Add(costQuantity.Denominator);
            }
        }

        return assets.ToList();
    }
}
