using System;
using System.Threading.Tasks;
using ZES.Infrastructure;
using ZES.Infrastructure.Domain;
using ZES.Interfaces.Branching;
using ZES.Interfaces.Domain;

namespace Chronos.Accounts.Queries;

[Transient]
public class CombinedAccountStatsQueryHandler(IProjectionManager manager, ITimeline activeTimeline,
    IQueryHandler<CombinedAccountStateQuery, AccountStatsState> accountStatsStateHandler,
    IQueryHandler<AccountStatsQuery, AccountStats> accountStatsHandler)
    : DefaultQueryHandler<CombinedAccountStatsQuery, AccountStats, NullState>(manager, activeTimeline)
{
    // do not read any streams for the query itself
    protected override Task<AccountStats> Handle(CombinedAccountStatsQuery query)
    {
        Predicate = s => false;
        return base.Handle(query);
    }

    protected override async Task<AccountStats> Handle(IProjection<NullState> projection, CombinedAccountStatsQuery query)
    {
        var accounts = query.Accounts;
        var state = await accountStatsStateHandler.Handle(new CombinedAccountStateQuery(accounts)
        {
            Timeline = query.Timeline,
            Timestamp = query.Timestamp,
        });
        
        return await accountStatsHandler.Handle(state, new AccountStatsQuery("Combined", query.Denominator)
        {
            Timeline = query.Timeline,
            Timestamp = query.Timestamp,
            QueryNet = query.QueryNet,
            EnforceCache = query.EnforceCache,
            NumberOfMatchingDays = query.NumberOfMatchingDays,
            AssetQuoteOverrides = query.AssetQuoteOverrides,
            TrackDisposalLots = query.TrackDisposalLots,
        }); 
    }
}