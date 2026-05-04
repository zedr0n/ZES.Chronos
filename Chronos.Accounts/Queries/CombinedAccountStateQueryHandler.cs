using System.Threading.Tasks;
using ZES.Infrastructure;
using ZES.Infrastructure.Domain;
using ZES.Interfaces.Branching;
using ZES.Interfaces.Domain;

namespace Chronos.Accounts.Queries;

[Transient]
public class CombinedAccountStateQueryHandler(IProjectionManager manager, ITimeline activeTimeline, IQueryHandler<AccountStatsStateQuery, AccountStatsState> accountStatsStateHandler)
    : DefaultQueryHandler<CombinedAccountStateQuery, AccountStatsState, NullState>(manager, activeTimeline)
{
    protected override Task<AccountStatsState> Handle(CombinedAccountStateQuery query)
    {
        Predicate = s => false;
        return base.Handle(query);
    }

    protected override async Task<AccountStatsState> Handle(IProjection<NullState> projection,
        CombinedAccountStateQuery query)
    {
        AccountStatsState state = null;

        foreach (var account in query.Accounts)
        {
            var accountStatsState = await accountStatsStateHandler.Handle(new AccountStatsStateQuery(account)
            {
                Timeline = query.Timeline,
                Timestamp = query.Timestamp,
            });
            
            state = state == null ? accountStatsState.Copy() : state.CombineWith(accountStatsState);
        }
        
        state?.AccountName = "Combined";
        return state;
    }
}