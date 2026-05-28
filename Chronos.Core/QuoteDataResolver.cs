using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZES.Interfaces.Clocks;

namespace Chronos.Core;

/// <summary>
/// Resolves quote data for specified assets and timestamps by delegating requests to appropriate data sources.
/// </summary>
public class QuoteDataResolver(IEnumerable<IQuoteDataResolverSource> sources) : IQuoteDataResolver
{
    /// <inheritdoc/>
    public Task<IReadOnlyDictionary<Time, double>> ResolveValues(QuoteDataRequest request)
    {
        var source = sources.Where(r => r.CanResolve(request)).OrderByDescending(r => r.Priority).FirstOrDefault();
        if (source == null)
            throw new InvalidOperationException($"No source can resolve the request for {request.ForAsset.AssetId}/{request.DomAsset.AssetId} for {request.Timestamps.Max()}");
        
        return source.ResolveValues(request);
    }
}