using Newtonsoft.Json;
using ZES.Infrastructure.Domain;

namespace Chronos.Core.Events;

/// <summary>
/// Represents an event where an asset pair has been registered.
/// </summary>
/// <remarks>
/// An asset pair is composed of two assets, one representing the "foreign" asset and the other representing the "domestic" asset.
/// This event contains the asset pair identifier and its associated assets.
/// </remarks>
[method: JsonConstructor]
public class AssetPairRegistered(string fordom, Asset forAsset, Asset domAsset, bool supportsIntraday = true) : Event
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AssetPairRegistered"/> class.
    /// Event that signifies the registration of an asset pair.
    /// </summary>
    public AssetPairRegistered()
        : this(null, null, null) { }
    
    /// <summary>
    /// Gets the identifier representing a combination of two assets, commonly referred to
    /// as the "foreign" and "domestic" assets. This property serves as a unique key to
    /// differentiate asset pairs in relevant contexts such as events, projections, or processes.
    /// </summary>
    /// <remarks>
    /// "Fordom" typically follows a naming convention based on its constituent assets.
    /// It is primarily used in asset pair registration, event application, and query handling
    /// to maintain relationships between assets in trading or exchange scenarios.
    /// </remarks>
    public string Fordom => fordom;

    /// <summary>
    /// Gets the "foreign" asset in an asset pair.
    /// </summary>
    /// <remarks>
    /// An asset pair consists of two assets: a "foreign" asset and a "domestic" asset.
    /// This property refers to the asset identified as the "foreign" component of the pair.
    /// </remarks>
    public Asset ForAsset => forAsset;

    /// <summary>
    /// Gets the "domestic" asset in an asset pair.
    /// </summary>
    /// <remarks>
    /// The domestic asset refers to the local or base asset in the context of the asset pair.
    /// It is one of the two assets that make up the asset pair, the other being the "foreign" asset.
    /// </remarks>
    public Asset DomAsset => domAsset;

    /// <summary>
    /// Gets a value indicating whether the registered asset pair supports intraday operations, such as trading or data analysis
    /// within a single trading day.
    /// </summary>
    /// <remarks>
    /// When set to true, this property signifies that the associated asset pair allows intraday-level
    /// activities, enabling more granular interactions such as real-time updates, analytics, and short-term
    /// trades. This can be a practical feature for markets or assets with high volatility or frequent market movement.
    /// </remarks>
    public bool SupportsIntraday => supportsIntraday; 
}