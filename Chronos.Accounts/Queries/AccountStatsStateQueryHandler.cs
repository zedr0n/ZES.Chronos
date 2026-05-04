using System.Threading.Tasks;
using Chronos.Core;
using ZES.Infrastructure;
using ZES.Infrastructure.Domain;
using ZES.Interfaces.Branching;
using ZES.Interfaces.Domain;

namespace Chronos.Accounts.Queries;

[Transient]
public class AccountStatsStateQueryHandler(IProjectionManager manager, ITimeline activeTimeline)
    : DefaultQueryHandler<AccountStatsStateQuery, AccountStatsState>(manager, activeTimeline)
{
    protected override async Task<AccountStatsState> Handle(AccountStatsStateQuery query)
    {
        Predicate = s => (s.Type == nameof(Account) && s.SameId(query.Account)) || s.Type == nameof(AssetPair) || s.Type == nameof(Transfer);
        return await base.Handle(query, query.Account);
    }
}