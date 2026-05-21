using System;
using System.Linq;
using System.Threading.Tasks;
using Chronos.Core.Queries;
using ZES.Infrastructure;
using ZES.Infrastructure.Domain;
using ZES.Interfaces.Branching;
using ZES.Interfaces.Domain;

namespace Chronos.Accounts.Queries;

[Transient]
public class CombinedAccountStatsQueryHandler(IProjectionManager manager, ITimeline activeTimeline,
    IQueryHandler<CombinedAccountStateQuery, AccountState> accountStatsStateHandler,
    IQueryHandler<AccountStatsQuery, AccountStats> accountStatsHandler,
    IQueryHandler<AssetQuoteQuery, AssetQuote> assetQuoteHandler)
    : DefaultQueryHandler<CombinedAccountStatsQuery, AccountStats, NullState>(manager, activeTimeline)
{
    // do not read any streams for the query itself
    protected override Task<AccountStats> Handle(CombinedAccountStatsQuery query)
    {
        Predicate = s => false;
        return base.Handle(query);
    }

    protected override async Task<AccountStats> Handle(IProjectionState<NullState> projection, CombinedAccountStatsQuery query)
    {
        var accounts = query.Accounts;
        var state = await accountStatsStateHandler.Handle(new CombinedAccountStateQuery(accounts)
        {
            Timeline = query.Timeline,
            Timestamp = query.Timestamp,
            AdditionalTimestamps = query.AdditionalTimestamps
        });
        
        var accountStatsQuery = new AccountStatsQuery(state.AccountName, query.Denominator)
        {
            Timeline = query.Timeline,
            Timestamp = query.Timestamp,
            QueryNet = query.QueryNet,
            EnforceCache = query.EnforceCache,
            NumberOfMatchingDays = query.NumberOfMatchingDays,
            AssetQuoteOverrides = query.AssetQuoteOverrides,
            TrackDisposalLots = query.TrackDisposalLots,
            ComputeCapitalGains = query.ComputeCapitalGains
        };

        var stats = await accountStatsHandler.Handle(state, accountStatsQuery);
        
        var assets = accountStatsQuery.AssetQuotes.Keys.Select(k => k.asset).Distinct().ToList();
        foreach (var asset in assets)
        {
            var quotes = await assetQuoteHandler.Handle(new AssetQuoteQuery(asset, query.Denominator)
            {
                Timeline = query.Timeline,
                Timestamp = query.Timestamp,
                EnforceCache = query.EnforceCache,
                UpdateQuote = query.QueryNet,
                AdditionalTimestamps = query.AdditionalTimestamps,
                AssetQuoteOverrides = query.AssetQuoteOverrides
            });
            foreach (var (t, quote) in quotes.HistoricalResults)
            {
                accountStatsQuery.AssetQuotes[(asset, query.Denominator, t)] = quote;
            }
        }
        
        foreach (var t in query.AdditionalTimestamps ?? [])
        {
            accountStatsQuery.Timestamp = t;
            stats.HistoricalResults[t] = await accountStatsHandler.Handle(state.HistoricalResults[t], accountStatsQuery);
        }

        return stats; 
    }
}
