using Newtonsoft.Json;
using ZES.Infrastructure.Domain;
namespace Chronos.Core.Queries;

/// <summary>
/// Represents a query to retrieve information about an asset pair.
/// </summary>
/// <remarks>
/// This query is used to fetch detailed information about a specific asset pair,
/// identified by the combination of a base asset and a quote asset.
/// </remarks>
[method: JsonConstructor]
public class AssetPairInfoQuery(string fordom) : SingleQuery<AssetPairInfo>(fordom)
{
}