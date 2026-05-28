using System.Collections.Generic;
using System.Threading.Tasks;
using Chronos.Core.Net;
using ZES.Interfaces.Clocks;

namespace Chronos.Core;

/// <summary>
/// Represents a source interface that defines methods to resolve quote data for specific assets and timestamps.
/// </summary>
public interface IQuoteDataResolverSource
{
    /// <summary>
    /// Gets the priority of the quote data resolver source.
    /// A higher value indicates a higher precedence for resolving quote data.
    /// </summary>
    int Priority { get; }
    
    /// <summary>
    /// Determines whether the specified quote data request can be resolved.
    /// </summary>
    /// <param name="request">The quote data request to check for resolvability.</param>
    /// <returns><c>true</c> if the request can be resolved; otherwise, <c>false</c>.</returns>
    bool CanResolve(QuoteDataRequest request);
    
    /// <summary>
    /// Resolves quote data values for a specified asset, using provided parameters such as timestamps, ticker, and URL overrides.
    /// </summary>
    /// <param name="request">Quote data request</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a read-only dictionary mapping timestamps to resolved quote values.</returns>
    Task<IReadOnlyDictionary<Time, double>> ResolveValues(QuoteDataRequest request);
}