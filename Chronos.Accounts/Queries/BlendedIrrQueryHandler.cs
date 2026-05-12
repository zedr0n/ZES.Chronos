using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Chronos.Core;
using NodaTime;
using ZES.Infrastructure;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.Utils;
using ZES.Interfaces.Branching;
using ZES.Interfaces.Domain;

namespace Chronos.Accounts.Queries;

[Transient]
public class BlendedIrrQueryHandler : DefaultQueryHandler<BlendedIrrQuery, BlendedIrr, BlendedIrrState>
{
    private readonly ITimeline _activeTimeline;
    private readonly IQueryHandler<CombinedAccountStatsQuery, AccountStats> _combinedAccountStatsHandler;

    public BlendedIrrQueryHandler(IProjectionManager manager, ITimeline activeTimeline, 
        IQueryHandler<CombinedAccountStatsQuery, AccountStats> combinedAccountStatsHandler)
        : base(manager, activeTimeline)
    {
        _activeTimeline = activeTimeline;
        _combinedAccountStatsHandler = combinedAccountStatsHandler;
    }

    protected override Task<BlendedIrr> Handle(BlendedIrrQuery query)
    {
        Predicate = s => false;
        return base.Handle(query);
    }

    protected override async Task<BlendedIrr> Handle(IProjection<BlendedIrrState> projection, BlendedIrrQuery query)
    {
        var accounts = query.Accounts;
        var allCashflows = new List<(Instant time, Quantity quantity)>();

        var t0 = query.Start;
        var startBalance = 0.0;

        if (t0 != default)
        {
            var startAccountStats =
                await _combinedAccountStatsHandler.Handle(new CombinedAccountStatsQuery(accounts, query.Denominator)
                {
                    Timeline = query.Timeline,
                    Timestamp = t0.ToTime(),
                    QueryNet = query.QueryNet,
                    ComputeCapitalGains = false
                });
            startBalance += startAccountStats.Balance.Amount;
            allCashflows.AddRange(startAccountStats.ExternalCashflows.Where(x => x.Item1 >= t0));
        }
        
        if (startBalance != 0.0)
            allCashflows.Add((t0, new Quantity(-startBalance, query.Denominator)));
        
        var endBalance = 0.0;
        
        var endAccountStats =
            await _combinedAccountStatsHandler.Handle(new CombinedAccountStatsQuery(accounts, query.Denominator)
            {
                Timeline = query.Timeline,
                Timestamp = query.Timestamp,
                QueryNet = query.QueryNet,
                ComputeCapitalGains = false 
            });
        endBalance += endAccountStats.Balance.Amount;
        allCashflows.AddRange(endAccountStats.ExternalCashflows.Where(x => x.Item1 >= t0));
        
        var now = query.Timestamp?.ToInstant() ?? _activeTimeline.Now.ToInstant();
        var extCashflows = allCashflows.OrderBy(x => x.time).Select(x => (x.time, x.quantity.Amount)).ToList();
        extCashflows.Add((now, endBalance));
        
        var irr = IrrSolver.Solve(extCashflows);
        return new BlendedIrr(irr);
    }
}