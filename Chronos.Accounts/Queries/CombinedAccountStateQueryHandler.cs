using System.Threading.Tasks;
using ZES.Infrastructure;
using ZES.Infrastructure.Domain;
using ZES.Interfaces.Branching;
using ZES.Interfaces.Domain;

namespace Chronos.Accounts.Queries;

[Transient]
public class CombinedAccountStateQueryHandler(IProjectionManager manager, ITimeline activeTimeline, IQueryHandler<AccountStateQuery, AccountState> accountStatsStateHandler)
    : DefaultQueryHandler<CombinedAccountStateQuery, AccountState, NullState>(manager, activeTimeline)
{
    protected override Task<AccountState> Handle(CombinedAccountStateQuery query)
    {
        Predicate = s => false;
        return base.Handle(query);
    }

    protected override async Task<AccountState> Handle(IProjectionState<NullState> projection,
        CombinedAccountStateQuery query)
    {
        AccountState state = null;

        foreach (var account in query.Accounts)
        {
            var accountStatsState = await accountStatsStateHandler.Handle(new AccountStateQuery(account)
            {
                Timeline = query.Timeline,
                Timestamp = query.Timestamp,
                AdditionalTimestamps = query.AdditionalTimestamps
            });
            
            state = state == null ? accountStatsState.Copy() : state.CombineWith(accountStatsState);
        }
        
        state?.AccountName = "Combined";
        return state;
    }
}