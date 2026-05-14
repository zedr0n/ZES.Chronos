using System.Threading.Tasks;
using ZES.Infrastructure.Domain;
using ZES.Interfaces.Branching;
using ZES.Interfaces.Domain;

namespace Chronos.Accounts.Queries;

public class AssetLedgerQueryHandler(IProjectionManager manager, ITimeline activeTimeline)
    : DefaultQueryHandler<AssetLedgerQuery, AssetLedger>(manager, activeTimeline)
{
    protected override async Task<AssetLedger> Handle(AssetLedgerQuery query)
    {
        Predicate = s => s.Type is nameof(Account) or nameof(Transfer);
        return await base.Handle(query);
    }
}