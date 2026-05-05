using System;
using Newtonsoft.Json;

namespace Chronos.Core;

/// <summary>
/// Represents an override for an asset quote, providing a specific price for a specified source and currency pair.
/// </summary>
/// <param name="SourceOperationId">The unique identifier of the source of the quote.</param>
/// <param name="Fordom">The currency pair for which the quote is being overridden.</param>
/// <param name="Price">The price value to be used for the specified currency pair.</param>
/// <remarks>
/// This record is used to define a custom quote for an asset, overriding default or externally obtained values.
/// It is immutable, ensuring that the quote values remain consistent once instantiated.
/// </remarks>
[method: JsonConstructor]
public record AssetQuoteOverride(string SourceOperationId, string Fordom, double Price)
{
    /// <summary>
    /// Returns a string that represents the current AssetQuoteOverride object.
    /// </summary>
    /// <returns>A string representation of the AssetQuoteOverride object, including values for SourceId, Fordom, and Price.</returns>
    public override string ToString()
    {
        return $"AssetQuoteOverride(sourceId: {SourceOperationId}, fordom: {Fordom}, price: {Price})";
    }
}