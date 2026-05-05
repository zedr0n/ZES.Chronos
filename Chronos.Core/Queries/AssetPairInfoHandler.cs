using System.Collections.Generic;
using System.Linq;
using Chronos.Core.Events;
using NodaTime;
using ZES.Interfaces;
using ZES.Interfaces.Domain;

namespace Chronos.Core.Queries;

/// <summary>
/// Handles projections for the <see cref="AssetPairInfo"/> state based on domain events.
/// Processes events such as <see cref="AssetPairRegistered"/>, <see cref="QuoteTickerAdded"/>,
/// and <see cref="QuoteAdded"/> to update or create the corresponding state representation.
/// Implements the <see cref="IProjectionHandler{T}"/> interface.
/// </summary>
public class AssetPairInfoHandler : IProjectionHandler<AssetPairInfo>
{
    /// <inheritdoc/>
    public AssetPairInfo Handle(IEvent e, AssetPairInfo state) =>
        Handle((dynamic)e, state);

    public AssetPairInfo Handle(AssetPairRegistered e, AssetPairInfo state) =>
        new(e.ForAsset, e.DomAsset, [], state.Ticker, e.HolidayCalendar, e.SupportsIntraday); 

    public AssetPairInfo Handle(QuoteTickerAdded e, AssetPairInfo state) =>
        new(state.ForAsset, state.DomAsset, state.QuoteDates, e.Ticker, state.HolidayCalendar, state.SupportsIntraday);

    public AssetPairInfo Handle(QuoteAdded e, AssetPairInfo state)
    {
        var dates = new HashSet<Instant>(state.QuoteDates) { e.Date };
        var newState = new AssetPairInfo(state.ForAsset, state.DomAsset, dates.ToArray(), state.Ticker, state.HolidayCalendar, state.SupportsIntraday);
        return newState;
    }
}