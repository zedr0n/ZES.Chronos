using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Chronos.Core;
using NodaTime;
using ZES.Infrastructure;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.Utils;
using ZES.Interfaces.Branching;
using ZES.Interfaces.Clocks;
using ZES.Interfaces.Domain;

namespace Chronos.Accounts.Queries;

[Transient]
public class BlendedIrrQueryHandler : DefaultQueryHandler<BlendedIrrQuery, BlendedIrr, BlendedIrrState>
{
    private readonly IActiveTimeline _activeTimeline;
    private readonly IQueryHandler<CombinedAccountStatsQuery, AccountStats> _combinedAccountStatsHandler;

    public BlendedIrrQueryHandler(IProjectionManager manager, IActiveTimeline activeTimeline, 
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

    protected override async Task<BlendedIrr> Handle(IProjectionState<BlendedIrrState> projection, BlendedIrrQuery query)
    {
        var accounts = query.Accounts;

        var t0 = query.Start;

        if(query.Period != null && query.Start != default)
            throw new InvalidOperationException("Cannot specify both period and start time");

        var now = query.Timestamp ?? _activeTimeline.Now;
        var additionalTimestamps = query.AdditionalTimestamps?.ToList() ?? [];
        if (t0 != default)
        {
            additionalTimestamps.Add(t0.ToTime());
        }
        else if (query.Period != null)
        {
            foreach (var t in additionalTimestamps.ToList())
                additionalTimestamps.Add(t.MinusPeriod(query.Period));
            t0 = now.MinusPeriod(query.Period).ToInstant();
            additionalTimestamps.Add(t0.ToTime());
        }
        additionalTimestamps = additionalTimestamps.Distinct().OrderBy(x => x).ToList();
        
        var accountStats = await _combinedAccountStatsHandler.Handle(
            new CombinedAccountStatsQuery(accounts, query.Denominator)
            {
                Timeline = query.Timeline,
                Timestamp = query.Timestamp,
                UpdateQuotes = query.UpdateQuotes,
                ComputeCapitalGains = false,
                AdditionalTimestamps = additionalTimestamps 
            });

        var irr = ComputeIrr((t0, accountStats.HistoricalResults.GetValueOrDefault(t0.ToTime(), null)),
            (now.ToInstant(), accountStats), query.Denominator);
        
        var blendedIrr = new BlendedIrr(irr);

        foreach (var t in query.AdditionalTimestamps ?? [])
        {
            var startT = query.Period != null ? t.MinusPeriod(query.Period).ToInstant() : query.Start;
            if (startT > t.ToInstant())
                throw new InvalidOperationException($"Start time {startT} is after the end time {t}");
            var historicalIrr =  ComputeIrr(
                (startT, accountStats.HistoricalResults.GetValueOrDefault(startT.ToTime(), null)),
                (t.ToInstant(), accountStats.HistoricalResults.GetValueOrDefault(t, null)),
                query.Denominator); 
            blendedIrr.HistoricalResults[t] = new BlendedIrr(historicalIrr);
        }

        return blendedIrr;
    }

    double ComputeIrr((Instant t, AccountStats stats) start, (Instant t, AccountStats stats) end, Asset denominator)
    {
        var allCashflows = new List<(Instant time, Quantity quantity)>();

        if (start.t != default)
            allCashflows.Add((start.t, new Quantity(-start.stats.Balance.Amount, denominator)));
        
        var endBalance = end.stats.Balance.Amount;
        allCashflows.AddRange(end.stats.ExternalCashflows.Where(x => x.Item1 > start.t));
       
        var extCashflows = allCashflows.OrderBy(x => x.time).Select(x => (x.time, x.quantity.Amount)).ToList();
        extCashflows.Add((end.t, endBalance));
        
        return IrrSolver.Solve(extCashflows);
    }
}