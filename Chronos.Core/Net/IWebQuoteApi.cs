using System;
using System.Collections.Generic;
using ZES.Interfaces.Clocks;
using ZES.Interfaces.Net;

namespace Chronos.Core.Net;

/// <summary>
/// An interface that defines methods for interacting with a web API.
/// </summary>
public interface IWebQuoteApi
{
    /// <summary>
    /// Constructs the URL for a specific ticker and time, with an optional cache enforcement setting.
    /// </summary>
    /// <param name="ticker">The identifier representing the target entity or data set.</param>
    /// <param name="date">The specific point in time for the requested data.</param>
    /// <param name="enforceCache">A boolean flag indicating whether to enforce caching mechanisms.</param>
    /// <returns>A string representing the constructed URL.</returns>
    string GetUrl(string ticker, Time date = null, bool enforceCache = false);

    /// <summary>
    /// Constructs the URL for a specific ticker and a collection of dates, with an optional cache enforcement setting.
    /// </summary>
    /// <param name="ticker">The identifier representing the target entity or data set.</param>
    /// <param name="dates">A collection of specific points in time for the requested data.</param>
    /// <param name="enforceCache">A boolean flag indicating whether to enforce caching mechanisms.</param>
    /// <returns>A string representing the constructed URL.</returns>
    string GetUrl(string ticker, IEnumerable<Time> dates, bool enforceCache = false);
    
    /// <summary>
    /// Gets a more precise quote URL for the specified ticker and timestamp when the provider supports it
    /// (for example, minute-level data for historical timestamps).
    /// </summary>
    /// <param name="ticker">The asset ticker symbol.</param>
    /// <param name="date">The specific timestamp for the query, or null to use the latest available data.</param>
    /// <param name="enforceCache">A boolean flag indicating whether to enforce caching mechanisms.</param>
    /// <returns>
    /// The constructed URL for a precise web API request.
    /// Return <c>null</c> when no precise endpoint can be produced.
    /// Implementations may also return the same URL as <c>GetUrl</c> when no distinct precise endpoint exists.
    /// </returns>
    string GetPreciseUrl(string ticker, Time date = null, bool enforceCache = false);

    /// <summary>
    /// Constructs a precise URL for a specific ticker and time, with an optional cache enforcement setting.
    /// </summary>
    /// <param name="ticker">The identifier representing the target entity or data set.</param>
    /// <param name="dates">A list of specific points in time for the requested data.</param>
    /// <param name="enforceCache">A boolean flag indicating whether to enforce caching mechanisms.</param>
    /// <returns>
    /// A string representing the constructed precise URL for the web API request.
    /// Returns <c>null</c> if no precise endpoint is available or when a precise URL cannot be determined.
    /// May return the same result as <c>GetUrl</c> if no distinct precise endpoint exists.
    /// </returns>
    string GetPreciseUrl(string ticker, List<Time> dates, bool enforceCache = false);

    /// <summary>
    /// Retrieves a value from the provided JSON result.
    /// </summary>
    /// <param name="result">The JSON result containing the data from which the value is to be extracted.</param>
    /// <returns>A double value extracted from the JSON result.</returns>
    (Time Timestamp, double Value) GetValue(IJsonResult result);

    /// <summary>
    /// Extracts a collection of timestamped values from the given JSON result.
    /// </summary>
    /// <param name="result">The JSON response containing the data to extract.</param>
    /// <returns>A collection of tuples, where each tuple contains a timestamp and a corresponding value.</returns>
    IEnumerable<(Time Timestamp, double Value)> GetValues(IJsonResult result);

    /// <summary>
    /// Retrieves the search ticker corresponding to the specified asset and domestic asset.
    /// </summary>
    /// <param name="forAsset">The target asset for which the search ticker is to be obtained.</param>
    /// <param name="domAsset">The domestic asset used to contextualize the search ticker.</param>
    /// <returns>A string representing the search ticker for the specified asset and domestic asset.</returns>
    string GetSearchTicker(Asset forAsset, Asset domAsset);

    /// <summary>
    /// Retrieves the type representing the JSON result structure expected from the API.
    /// </summary>
    /// <returns>A Type object describing the structure of the JSON result.</returns>
    Type GetJsonResultType();

    /// <summary>
    /// Determines whether the API can handle the specified combination of asset types and intraday setting.
    /// </summary>
    /// <param name="forAssetType">The asset type representing the foreign asset.</param>
    /// <param name="domAssetType">The asset type representing the domestic asset.</param>
    /// <param name="intraday">A boolean flag indicating whether the request is for intraday data.</param>
    /// <returns>A boolean value indicating whether the API can handle the specified combination of asset types and intraday setting.</returns>
    bool CanHandle(AssetType forAssetType, AssetType domAssetType, bool intraday);

    /// <summary>
    /// Gets a value indicating whether the API supports handling multiple timestamps in a single request.
    /// </summary>
    /// <returns>A boolean value indicating whether multiple timestamps are supported.</returns>
    bool SupportsMultipleTimestamps();
}
