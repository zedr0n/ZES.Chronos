using System;
using System.Threading.Tasks;
using Chronos.Core.Net;

namespace Chronos.Core;

/// <summary>
/// Provides functionality to execute JSON-based request operations for quote data.
/// </summary>
public interface IQuoteJsonRequestExecutor
{
    /// <summary>
    /// Gets the type of the JSON result returned by the executor.
    /// </summary>
    public Type JsonResultType { get; }

    /// <summary>Executes a request to retrieve JSON results for quotes based on the specified parameters.</summary>
    /// <param name="requestorId">
    /// A unique identifier for the requestor, used to track and distinguish different requests.
    /// </param>
    /// <param name="url">
    /// The URL from which the JSON data is to be retrieved.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation, containing the JSON result of type <see cref="IWebQuoteJsonResult"/>.
    /// </returns>
    Task<IWebQuoteJsonResult> Request(string requestorId, string url);
}

/// <inheritdoc />
public interface IQuoteJsonRequestExecutor<T> : IQuoteJsonRequestExecutor
    where T : class, IWebQuoteJsonResult
{
}