using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using ZES.Interfaces.Clocks;

namespace Chronos.Core.Queries;

/// <summary>
/// Represents a query for retrieving asset quote information from a data source.
/// </summary>
[method: JsonConstructor]
public class AssetQuoteQuery(Asset forAsset, Asset domAsset) : ZES.Infrastructure.Domain.Query<AssetQuote>
{
    /// <summary>
    /// Gets the asset for which the quote is being queried.
    /// This property identifies the base asset in the context of a currency pair or trading pair query.
    /// </summary>
    public Asset ForAsset => forAsset;

    /// <summary>
    /// Gets the domestic asset in a quote query context.
    /// This asset typically serves as the counter asset used to derive exchange rates or prices relative to the specified asset.
    /// </summary>
    public Asset DomAsset => domAsset;

    /// <summary>
    /// Gets or sets a value indicating whether indicates whether the asset quote should be updated during the query execution.
    /// Setting this property to true will trigger an update of the quote data from the relevant source,
    /// ensuring that the query retrieves the most recent market information.
    /// </summary>
    public bool UpdateQuote { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether indicates whether to enforce the usage of cached data when resolving quotes.
    /// If set to true, cached data is used exclusively, bypassing real-time data retrieval.
    /// Otherwise, the system may attempt to fetch updated data based on the query context.
    /// </summary>
    public bool EnforceCache { get; set; }
   
    /// <summary>
    /// Gets or sets optional operation id used to resolve operation-scoped quote overrides.
    /// Normal market quote queries leave this unset.
    /// </summary>
    public string SourceOperationId { get; set; } 
    
    /// <summary>
    /// Gets or sets operation-scoped asset quote overrides used while valuing account history.
    /// </summary>
    /// <remarks>
    /// Overrides are passed through to asset quote resolution so transaction-specific prices can participate in normal
    /// direct, inverse, or triangulated quote paths.
    /// </remarks>
    public List<AssetQuoteOverride> AssetQuoteOverrides { get; set; }

    /// <summary>
    /// Gets or sets the collection of single asset quotes, indexed by a tuple containing the base asset,
    /// denominating asset, and timestamp. This property stores cached quotes and supports efficient access
    /// to historical or real-time pricing data for specific asset pairs and timestamps.
    /// </summary>
    [JsonIgnore]
    public Dictionary<(Asset ForAsset, Asset DomAsset, Time Timestamp), SingleAssetQuote> SingleAssetQuotes { get; set; }
}