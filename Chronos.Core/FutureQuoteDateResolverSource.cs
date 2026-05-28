using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZES.Infrastructure.Utils;
using ZES.Interfaces.Clocks;

namespace Chronos.Core;

/// <summary>
/// Provides a source implementation for resolving quote data for future dates using a clock-based approach.
/// </summary>
public class FutureQuoteDateResolverSource(IClock clock, DefaultQuoteDataResolverSource defaultResolver) : IQuoteDataResolverSource
{
    /// <inheritdoc/>
    public int Priority { get; } = 1;

    /// <inheritdoc/>
    public bool CanResolve(QuoteDataRequest request)
    {
        var now = clock.GetCurrentInstant();
        return request.Timestamps.Any(t => t.Date() > now.Date());
    }

    /// <inheritdoc/>
    public async Task<IReadOnlyDictionary<Time, double>> ResolveValues(QuoteDataRequest request)
    {
        var now = clock.GetCurrentInstant();
        var map = new Dictionary<Time, Time>();
        foreach (var t in request.Timestamps.Where(t => t > now))
            map[t] = now;
            
        var nowRequest = request with { Timestamps = request.Timestamps.Select(t => map.GetValueOrDefault(t, t)).Distinct() };
        var values = await defaultResolver.ResolveValues(nowRequest);
        return request.Timestamps.ToDictionary(t => t, t => t > now ? values[now] : values[t]); 
    }
}