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

namespace Chronos.Accounts.Queries;

[Transient]
public class CapitalGainsQueryHandler(
    IProjectionManager manager,
    ITimeline activeTimeline,
    IQueryHandler<AssetQuoteQuery, AssetQuote> assetQuoteHandler,
    IQueryHandler<CombinedAccountStateQuery, AccountState> accountStateHandler,
    IQueryHandler<AssetLedgerQuery, AssetLedger> ledgerHandler,
    AssetPoolFactory assetPoolFactory)
    : DefaultQueryHandler<CapitalGainsQuery, CapitalGains, NullState>(manager, activeTimeline)
{
    /// <inheritdoc/>
    protected override async Task<CapitalGains> Handle(IProjectionState<NullState> projection, CapitalGainsQuery query)
    {
        if (projection == null)
            throw new ArgumentNullException(nameof(projection), $"{typeof(IProjection<AccountState>).Name}");
        var timestamp = query.Timestamp;
        var state = await accountStateHandler.Handle(new CombinedAccountStateQuery(query.Accounts)
        {
            Timeline = query.Timeline,
            Timestamp = timestamp,
            AdditionalTimestamps = query.AdditionalTimestamps
        });
            
        var capitalGains = await Handle(state, query);
        try
        {
            foreach (var t in query.AdditionalTimestamps ?? [])
            {
                query.Timestamp = t;
                capitalGains.HistoricalResults[t] = await Handle(state.HistoricalResults[t], query);
            }
        }
        finally
        {
            query.Timestamp = timestamp;
        }

        return capitalGains;
    }

    public override async Task<CapitalGains> Handle<TState>(TState tState, CapitalGainsQuery query)
    {
        if (tState is not AccountState state)
            throw new ArgumentException($"{nameof(AccountState)} expected", nameof(tState));

        var denominator = query.Denominator;
            
        var poolsDictionary = new Dictionary<Asset, IAssetPools>();
        var costBasisDictionary = new Dictionary<Asset, Quantity>();
        var realisedGainsDictionary = new Dictionary<Asset, Quantity>();
        var realisedGainsPerTaxYearDictionary = new Dictionary<Asset, Dictionary<int, Quantity>>();
        var disposalGainItemsDictionary = new Dictionary<Asset, List<DisposalGainItem>>();
        
        var assetTransferIn = state.GetAssetTransfersIn();
        var assetTransfersOut = state.GetAssetTransfersOut();
        var feeDisposals = state.FeeDisposals;
        
        var fromAccountStateDictionary = new Dictionary<(string account, Time time), AccountState>();
        var joinedAccountStateDictionary = new Dictionary<(string account, Time time), AccountState>();

        var assetLedger = await ledgerHandler.Handle(new AssetLedgerQuery() { Timeline = query.Timeline });
        if (assetLedger == null)
            throw new InvalidOperationException($"Asset ledger not found");
        
        var joinedStatsTimestampsByAccount = assetTransferIn
            .SelectMany(x => x.Value
                .Where(y => assetLedger.HasCrossAccountMatchingPair(
                    state.GetAccountNames(),
                    y.fromAccount,
                    y.quantity.Denominator,
                    x.Key,
                    query.NumberOfMatchingDays))
                .Select(y => new
                {
                    Account = y.fromAccount,
                    Timestamp = x.Key
                }))
            .GroupBy(x => x.Account)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.Timestamp).Distinct().OrderBy(x => x).ToList());

        var allJoinedTimestamps = joinedStatsTimestampsByAccount.Values
            .SelectMany(x => x)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        var sourceStates = await LoadAccountStates(state.GetAccountNames().ToList(), allJoinedTimestamps, query);
        foreach (var (fromAccount, timestamps) in joinedStatsTimestampsByAccount)
        {
            var fromStates = await LoadAccountStates([fromAccount], timestamps, query);

            foreach (var timestamp in timestamps)
            {
                var joinedState = sourceStates[timestamp].Copy().CombineWith(fromStates[timestamp]);
                joinedAccountStateDictionary[(fromAccount, timestamp)] = joinedState;
                fromAccountStateDictionary[(fromAccount, timestamp)] = fromStates[timestamp];
            }
        }
    
        var missingTimestampsByAccount = assetTransferIn
            .SelectMany(x => x.Value.Select(y => new
            {
                Account = y.fromAccount,
                Timestamp = x.Key
            }))
            .Where(x => !fromAccountStateDictionary.ContainsKey((x.Account, x.Timestamp)))
            .GroupBy(x => x.Account)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => x.Timestamp).Distinct().OrderBy(x => x).ToList());
        
        await FillStateCache(missingTimestampsByAccount, fromAccountStateDictionary, query);
        
        var allTimestamps = state.Costs.Keys?.Union(assetTransferIn.Select(x => x.Key)).Union(assetTransfersOut.Select(x => x.Key)).Union(feeDisposals.Keys) ?? [];
        
        // we now need to sort all the costs by timestamp so that asset-asset swaps can be handled correctly
        const double tolerance = 1e-8;
        var invalidAssetsAfterTransfer = new HashSet<Asset>();
        
        foreach (var t in allTimestamps.OrderBy(x => x))
        {
            var costs = state.GetCosts(t).ToList(); 
            var transfersIn = assetTransferIn.GetValueOrDefault(t, []);
            var transfersOut = assetTransfersOut.GetValueOrDefault(t, []);
            var assetsHandledByFullTransfer = new HashSet<Asset>();
            
            // handle transfers in
            foreach (var (fromAccount, q) in transfersIn)
            {
                var hasCrossAccountMatchingPair = assetLedger.HasCrossAccountMatchingPair(state.GetAccountNames(), fromAccount, q.Denominator, t, query.NumberOfMatchingDays);

                if (!fromAccountStateDictionary.TryGetValue((fromAccount, t), out var fromAccountState))
                {
                    fromAccountState = await accountStateHandler.Handle(new CombinedAccountStateQuery([fromAccount])
                    {
                        Timeline = query.Timeline,
                        Timestamp = t
                    });
                    if (fromAccountState == null)
                        throw new InvalidOperationException($"Account {fromAccount} not found");
                    fromAccountStateDictionary[(fromAccount, t)] = fromAccountState;
                }
                
                if (hasCrossAccountMatchingPair)
                {
                    //var isFullTransfer = Math.Abs(fromAccountStats.AssetPools[q.Denominator].TotalQuantity - q.Amount) < tolerance;
                    var isFullTransfer = fromAccountState.IsFullTransfer(q.Denominator, t, q.Amount, tolerance);
                    if (!isFullTransfer)
                    {
                        invalidAssetsAfterTransfer.Add(q.Denominator);
                        continue;
                    }

                    var joinedAccounts = state.GetAccountNames().Append(fromAccount).ToList();
                    if (!joinedAccountStateDictionary.TryGetValue((fromAccount, t), out var combinedState))
                    {
                        combinedState = await accountStateHandler.Handle(
                            new CombinedAccountStateQuery(joinedAccounts)
                            {
                                Timeline = query.Timeline,
                                Timestamp = t
                            });
                        if (combinedState == null)
                            throw new InvalidOperationException($"Account {fromAccount} not found");
                        joinedAccountStateDictionary[(fromAccount, t)] = combinedState;
                    }
                        
                    var capitalGains = await Handle(combinedState,
                        new CapitalGainsQuery(joinedAccounts, denominator)
                    {
                        Timeline = query.Timeline,
                        Timestamp = t,
                        QueryNet = query.QueryNet,
                        EnforceCache = query.EnforceCache,
                        IncludeTransfersOutAtQueryDate = false,
                        NumberOfMatchingDays = query.NumberOfMatchingDays,
                        AssetQuoteOverrides = query.AssetQuoteOverrides,
                        TrackDisposalLots = query.TrackDisposalLots,
                    });
                    
                    if(!capitalGains.AssetPools.TryGetValue(q.Denominator, out var joinedPool))
                        throw new InvalidOperationException($"Asset {q.Denominator} not found in joined pool");
                        
                    poolsDictionary[q.Denominator] = joinedPool;
                    costBasisDictionary[q.Denominator] = capitalGains.CostBasis[q.Denominator];
                    realisedGainsDictionary[q.Denominator] = capitalGains.RealisedGains[q.Denominator];
                    realisedGainsPerTaxYearDictionary[q.Denominator] = capitalGains.RealisedGainsPerTaxYear[q.Denominator];
                    disposalGainItemsDictionary[q.Denominator] = capitalGains.DisposalGainItems[q.Denominator];
                    assetsHandledByFullTransfer.Add(q.Denominator);
                }
                else
                {
                    var fromCapitalGains = await Handle(fromAccountState,
                        new CapitalGainsQuery([fromAccount], denominator)
                        {
                            Timeline = query.Timeline,
                            QueryNet = query.QueryNet,
                            Timestamp = t,
                            EnforceCache = query.EnforceCache,
                            IncludeTransfersOutAtQueryDate = false,
                            NumberOfMatchingDays = query.NumberOfMatchingDays,
                            AssetQuoteOverrides = query.AssetQuoteOverrides,
                            TrackDisposalLots = query.TrackDisposalLots,
                        });
                    
                    var fromPools = fromCapitalGains.AssetPools;
                    var pools = poolsDictionary.GetValueOrDefault(q.Denominator, assetPoolFactory.Create(query.NumberOfMatchingDays, query.TrackDisposalLots));
                    pools.TransferFrom(t, fromPools[q.Denominator], q.Amount);
                    poolsDictionary[q.Denominator] = pools;
                }
            }
             
            // all assets involved in the transactions on t
            var assets = costs.Select(c => c.assetQuantity.Denominator).Where(a => a.AssetType != AssetType.Currency)
                .Union(costs.Select(c => c.costQuantity.Denominator).Where(a => a.AssetType != AssetType.Currency) )
                .Where(a => !assetsHandledByFullTransfer.Contains(a)).ToList();
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
            
            // handle transfers out
            if(transfersOut.GroupBy(x => x.Denominator).Any(x => x.Count() > 1))
                throw new InvalidOperationException("Multiple transfers out to the same asset are not supported");
            
            foreach (var q in transfersOut)
            {
                var pools = poolsDictionary.GetValueOrDefault(q.Denominator, assetPoolFactory.Create(query.NumberOfMatchingDays, query.TrackDisposalLots));
                
                // Even when excluding the transfer-out itself at the query date, advance the
                // source pools to the transfer date so transfers-in receive a normalized state.                    
                if(t == query.Timestamp && !query.IncludeTransfersOutAtQueryDate)
                    pools.AdvanceTo(t);
                else
                    pools.TransferOut(t, q.Amount);
                poolsDictionary[q.Denominator] = pools;
            }

            // handle fee disposals
            foreach (var (fee, sourceOperationId) in state.GetFeeDisposals(t))
            {
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
            if (invalidAssetsAfterTransfer.Contains(asset))
            {
                costBasisDictionary[asset] = null;
                realisedGainsDictionary[asset] = null;
                realisedGainsPerTaxYearDictionary[asset] = null;
                disposalGainItemsDictionary[asset] = null;
            }
            else
            {
                var pools = poolsDictionary[asset];
                costBasisDictionary[asset] = new Quantity(pools.CostBasis, denominator);
                realisedGainsDictionary[asset] = new Quantity(pools.RealisedGain, denominator);
                realisedGainsPerTaxYearDictionary[asset] = pools.GetRealisedGainsPerTaxYear().ToDictionary(x => x.Key, x => new Quantity(x.Value, denominator));
                disposalGainItemsDictionary[asset] = pools.GetDisposalGains().Select(x => new DisposalGainItem(x)).ToList();
            }
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

    private async Task FillStateCache(Dictionary<string, List<Time>> timestampsByAccount, 
    Dictionary<(string account, Time timestamp), AccountState> cache,
    CapitalGainsQuery query)
    {
        foreach (var (account, timestamps) in timestampsByAccount)
        {
            var latest = timestamps[^1];
            var extraTimestamps = timestamps.Take(timestamps.Count - 1).ToList();
            var state =  await accountStateHandler.Handle(new CombinedAccountStateQuery([account])
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

    private async Task<Dictionary<Time, AccountState>> LoadAccountStates(List<string> accounts,
        List<Time> timestamps, CapitalGainsQuery query)
    {
        if(timestamps.Count == 0)
            return new Dictionary<Time, AccountState>();
        
        var latest = timestamps[^1];
        var extraTimestamps = timestamps.Take(timestamps.Count - 1).ToList();
        var accountStates = new Dictionary<Time, AccountState>();

        var state =  await accountStateHandler.Handle(new CombinedAccountStateQuery(accounts)
        {
            Timeline = query.Timeline,
            Timestamp = latest,
            AdditionalTimestamps = extraTimestamps
        }); 
        if (state == null)
            throw new InvalidOperationException($"Account in {accounts} not found");

        accountStates[latest] = state;
        foreach(var t in extraTimestamps)
            accountStates[t] = state.HistoricalResults[t];
        return accountStates;
    }
}