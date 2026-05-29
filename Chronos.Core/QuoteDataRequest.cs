using System.Collections.Generic;
using ZES.Interfaces.Clocks;

namespace Chronos.Core;

/// <summary>
/// Represents a data request for resolving quote values associated with specific assets and timestamps.
/// <param name="AssetPair">Gets the asset pair for which the quote data is requested.</param>
/// <param name="Timestamps">Gets the collection of timestamps for which the quote data is resolved.</param>
/// <param name="Ticker">Gets the ticker symbol associated with the asset.</param>
/// <param name="UrlOverride">Gets or sets an optional URL to override the default data source URL.</param>
/// <param name="EnforceCache">Gets or sets a value indicating whether the cache should be enforced during data resolution.</param>
/// <param name="UseBatching">Gets or sets a value determining whether batching should be used when resolving the data.</param>
/// </summary>
public record QuoteDataRequest(
    AssetPair AssetPair,
    IEnumerable<Time> Timestamps,
    string Ticker,
    string UrlOverride,
    bool EnforceCache,
    bool UseBatching
);