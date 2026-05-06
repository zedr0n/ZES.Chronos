using System.Collections.Generic;
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
public class RealisedGainsForTaxYearQueryHandler(IProjectionManager manager, ITimeline activeTimeline, IQueryHandler<AccountStatsQuery, AccountStats> accountStatsHandler)
    : DefaultQueryHandler<RealisedGainsForTaxYearQuery, RealisedGainsForTaxYear, NullState>(manager, activeTimeline)
{
    // do not read any streams for the query itself
    protected override Task<RealisedGainsForTaxYear> Handle(RealisedGainsForTaxYearQuery query)
    {
        Predicate = s => false;
        return base.Handle(query);
    }

    protected override async Task<RealisedGainsForTaxYear> Handle(IProjection<NullState> projection,
        RealisedGainsForTaxYearQuery query)
    {
        var timestamp = query.Timestamp;
        
        // Evaluate the next 30 days so UK matching can allocate later acquisitions
        // back to disposals in the requested tax year.
        if (timestamp != null)
            timestamp = timestamp.ToInstant().Plus(Duration.FromDays(30)).ToTime();
        
        var stats = await accountStatsHandler.Handle(new AccountStatsQuery(query.Account, query.Denominator)
        {
            Timeline = query.Timeline,
            Timestamp = timestamp,
            QueryNet = query.QueryNet,
            AssetQuoteOverrides = query.AssetQuoteOverrides
        });

        var realisedGainsPerTaxYear = stats.RealisedGainsPerTaxYear.GetValueOrDefault(query.Asset, new Dictionary<int, Quantity>());
        return new RealisedGainsForTaxYear(realisedGainsPerTaxYear.GetValueOrDefault(query.TaxYear, new Quantity(0, query.Denominator)));
    }
}
