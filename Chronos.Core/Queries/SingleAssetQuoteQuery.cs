using Newtonsoft.Json;
using ZES.Infrastructure.Domain;

namespace Chronos.Core.Queries;

/// <summary>
/// Represents a query to retrieve a single asset quote for a specific asset pair.
/// </summary>
[method: JsonConstructor]
public class SingleAssetQuoteQuery(string fordom) : SingleQuery<SingleAssetQuote>(fordom)
{
    /// <summary>
    /// Gets the identifier for the asset pair used in the single asset quote query.
    /// </summary>
    public string Fordom { get; } = fordom;
}