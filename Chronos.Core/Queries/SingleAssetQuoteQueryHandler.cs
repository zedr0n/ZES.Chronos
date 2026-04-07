using System.Threading.Tasks;
using ZES.Infrastructure.Domain;
using ZES.Interfaces.Branching;
using ZES.Interfaces.Domain;

namespace Chronos.Core.Queries;

/// <summary>
/// A query handler for resolving single asset quotes based on the specified currency pair and timestamp.
/// This class extends the <see cref="DefaultSingleQueryHandler{TQuery, TResult}"/> to handle
/// <see cref="SingleAssetQuoteQuery"/> and return a <see cref="SingleAssetQuote"/>.
/// </summary>
public class SingleAssetQuoteQueryHandler(IProjectionManager manager, ITimeline activeTimeline)
    : DefaultSingleQueryHandler<SingleAssetQuoteQuery, SingleAssetQuote>(manager, activeTimeline)
{
    private readonly ITimeline _activeTimeline = activeTimeline;

    /// <inheritdoc/>
    public override Task<SingleAssetQuote> Handle(IProjection projection, SingleAssetQuoteQuery query)
    {
        return query.Fordom switch
        {
            "GBXGBP" => Task.FromResult(new SingleAssetQuote(0.01, query.Timestamp?.ToInstant() ?? _activeTimeline.Now.ToInstant())),
            "GBPGBX" => Task.FromResult(new SingleAssetQuote(1.0 / 0.01, query.Timestamp?.ToInstant() ?? _activeTimeline.Now.ToInstant())),
            _ => base.Handle(projection, query),
        };
    }
}