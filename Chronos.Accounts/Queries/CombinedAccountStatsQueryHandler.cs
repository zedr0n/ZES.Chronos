using System;
using System.Threading.Tasks;
using ZES.Infrastructure;
using ZES.Infrastructure.Domain;
using ZES.Interfaces.Branching;
using ZES.Interfaces.Domain;

namespace Chronos.Accounts.Queries;

[Transient]
public class CombinedAccountStatsQueryHandler(IProjectionManager manager, ITimeline activeTimeline, IQueryHandler<AccountStatsQuery, AccountStats> accountStatsHandler)
    : DefaultQueryHandler<CombinedAccountStatsQuery, AccountStats, CombinedAccountStatsState>(manager, activeTimeline)
{
    // do not read any streams for the query itself
    protected override Task<AccountStats> Handle(CombinedAccountStatsQuery query)
    {
        Predicate = s => false;
        return base.Handle(query);
    }

    protected override async Task<AccountStats> Handle(IProjection<CombinedAccountStatsState> projection, CombinedAccountStatsQuery query)
    {
        var accounts = query.Accounts;
        AccountStatsState state = null;
        
        foreach(var account in accounts)
        {
            var accountStats = await accountStatsHandler.Handle(new AccountStatsQuery(account, query.Denominator)
            {
                Timeline = query.Timeline,
                Timestamp = query.Timestamp,
                QueryNet = query.QueryNet,
                NumberOfMatchingDays = query.NumberOfMatchingDays
            });

            state = state == null ? accountStats.State.Copy() : state.CombineWith(accountStats.State);
        }
        state?.AccountName = "Combined";

        return await accountStatsHandler.Handle(state, new AccountStatsQuery("Combined", query.Denominator)
        {
            Timeline = query.Timeline,
            Timestamp = query.Timestamp,
            QueryNet = query.QueryNet,
            NumberOfMatchingDays = query.NumberOfMatchingDays
        }); 
    }
}