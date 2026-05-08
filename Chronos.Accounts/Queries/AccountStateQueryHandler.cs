using System.Threading.Tasks;
using Chronos.Core;
using ZES.Infrastructure;
using ZES.Infrastructure.Domain;
using ZES.Interfaces.Branching;
using ZES.Interfaces.Domain;

namespace Chronos.Accounts.Queries;

[Transient]
public class AccountStateQueryHandler(IProjectionManager manager, ITimeline activeTimeline)
    : DefaultQueryHandler<AccountStateQuery, AccountState>(manager, activeTimeline)
{
    protected override async Task<AccountState> Handle(AccountStateQuery query)
    {
        Predicate = s => (s.Type == nameof(Account) && s.SameId(query.Account)) || s.Type == nameof(AssetPair) || s.Type == nameof(Transfer);
        return await base.Handle(query, query.Account);
    }
}