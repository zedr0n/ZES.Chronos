using System.Collections.Generic;
using System.Threading.Tasks;
using Chronos.Core.Net;
using ZES.Interfaces.Clocks;

namespace Chronos.Core;

/// <summary>
/// Provides functionality to resolve quote data values for a specified asset and its related details.
/// </summary>
public interface IQuoteDataResolver
{
    /// <summary>
    /// Resolves quote data values for a specified asset, using provided parameters such as timestamps, ticker, and URL overrides.
    /// </summary>
    /// <param name="request">Quote data request</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a read-only dictionary mapping timestamps to resolved quote values.</returns>
    Task<IReadOnlyDictionary<Time, double>> ResolveValues(QuoteDataRequest request);
}