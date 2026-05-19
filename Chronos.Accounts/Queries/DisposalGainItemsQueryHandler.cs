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
public class DisposalGainItemsQueryHandler(IProjectionManager manager, ITimeline activeTimeline,
    IQueryHandler<CapitalGainsQuery, CapitalGains> capitalGainsHandler)
    : DefaultQueryHandler<DisposalGainItemsQuery, DisposalGainItems, NullState>(manager, activeTimeline)
{
    // do not read any streams for the query itself
    protected override Task<DisposalGainItems> Handle(DisposalGainItemsQuery query)
    {
        Predicate = s => false;
        return base.Handle(query);
    }
    
    protected override async Task<DisposalGainItems> Handle(IProjectionState<NullState> projection,
        DisposalGainItemsQuery query)
    {
        var timestamp = query.Timestamp;

        var capitalGains = await capitalGainsHandler.Handle(new CapitalGainsQuery(query.Accounts, query.Denominator, [query.Asset])
        {
            Timeline = query.Timeline,
            Timestamp = timestamp,
            QueryNet = query.QueryNet,
            AssetQuoteOverrides = query.AssetQuoteOverrides,
            TrackDisposalLots = query.TrackDisposalLots,
            EnforceCache = query.EnforceCache
        });

        var disposalGains = capitalGains.DisposalGainItems.GetValueOrDefault(query.Asset, []);
        return new DisposalGainItems() { Items = disposalGains };
    } 
}