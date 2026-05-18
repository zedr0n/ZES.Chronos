using System;
using System.Collections.Generic;
using System.Linq;
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
        var state = new AccountState();
        
        foreach (var account in query.Accounts.Distinct())
        {
            var accountState = await accountStatsStateHandler.Handle(new AccountStateQuery(account)
            {
                Timeline = query.Timeline,
                Timestamp = query.Timestamp,
                AdditionalTimestamps = query.AdditionalTimestamps
            });
            
            if (accountState == null)
                throw new InvalidOperationException($"Account {account} not found");
            
            state = state.CombineWith(accountState);
        }
        return state; 
    }
}