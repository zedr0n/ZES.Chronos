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
    IQueryHandler<AccountStatsQuery, AccountStats> accountStatsHandler,
    IQueryHandler<CombinedAccountStatsQuery, AccountStats> combinedAccountStatsHandler)
    : DefaultQueryHandler<DisposalGainItemsQuery, DisposalGainItems, NullState>(manager, activeTimeline)
{
    // do not read any streams for the query itself
    protected override Task<DisposalGainItems> Handle(DisposalGainItemsQuery query)
    {
        Predicate = s => false;
        return base.Handle(query);
    }
    
    protected override async Task<DisposalGainItems> Handle(IProjection<NullState> projection,
        DisposalGainItemsQuery query)
    {
        var timestamp = query.Timestamp;

        var accounts = query.Accounts;
        AccountStats stats = null;
        switch (accounts.Count)
        {
            case 0:
                break; 
            case > 1:
                stats = await combinedAccountStatsHandler.Handle(new CombinedAccountStatsQuery(accounts.ToList(), query.Denominator)
                {
                    Timeline = query.Timeline,
                    Timestamp = timestamp,
                    QueryNet = query.QueryNet,
                    AssetQuoteOverrides = query.AssetQuoteOverrides,
                    TrackDisposalLots = query.TrackDisposalLots,
                    EnforceCache = query.EnforceCache
                });
                break;
            case 1:
                stats = await accountStatsHandler.Handle(new AccountStatsQuery(accounts[0], query.Denominator)
                {
                    Timeline = query.Timeline,
                    Timestamp = timestamp,
                    QueryNet = query.QueryNet,
                    AssetQuoteOverrides = query.AssetQuoteOverrides,
                    TrackDisposalLots = query.TrackDisposalLots,
                    EnforceCache = query.EnforceCache
                });
                break;
        }
        
        var disposalGains = stats?.DisposalGainItems.GetValueOrDefault(query.Asset, []);
        return new DisposalGainItems() { Items = disposalGains };
    } 
}