using System;
using System.Collections.Generic;
using ZES.Interfaces.Net;

namespace Chronos.Core.Net;

/// <summary>
/// Represents an interface for performing web search operations specific to exchanges and tickers.
/// </summary>
public interface IWebSearchApi
{
    /// <summary>
    /// Retrieves a collection of exchange names from the provided JSON result.
    /// </summary>
    /// <param name="result">The JSON result containing exchange data.</param>
    /// <returns>A collection of exchange names extracted from the JSON result.</returns>
    public IEnumerable<string> GetExchanges(IJsonResult result);

    /// <summary>
    /// Retrieves the ticker symbol from the given JSON result.
    /// </summary>
    /// <param name="result">The JSON result containing data from which the ticker symbol is extracted.</param>
    /// <returns>The ticker symbol extracted from the JSON result.</returns>
    public string GetTicker(IJsonResult result);

    /// <summary>
    /// Constructs a URL for the specified ticker symbol.
    /// </summary>
    /// <param name="ticker">The ticker symbol for which to generate the URL.</param>
    /// <returns>The constructed URL as a string.</returns>
    public string GetUrl(string ticker);
    
    /// <summary>
    /// Retrieves the type of the JSON result expected from the web search API.
    /// </summary>
    /// <returns>The .NET type representing the structure of the JSON result.</returns>
    Type GetJsonResultType();
}