using System.Collections.Generic;
using System.Text.Json.Serialization;
using Chronos.Core;
using ZES.Interfaces.Clocks;
using ZES.Interfaces.Domain;

namespace Chronos.Accounts.Queries;

public class CapitalGains : IHistoricalState, IHistoricalResults<CapitalGains>
{
    public CapitalGains() { }
    
    public List<Asset> Assets { get; set; }
    
    /// <summary>
    /// Gets or sets realised gains by asset, expressed in the query denominator.
    /// </summary>
    [JsonIgnore]
    public Dictionary<Asset, Quantity> RealisedGains { get; set; }

    /// <summary>
    /// Gets or sets remaining cost basis by asset, expressed in the query denominator.
    /// </summary>
    [JsonIgnore]
    public Dictionary<Asset, Quantity> CostBasis { get; set; }
    
    /// <summary>
    /// Gets or sets realised gains grouped by asset and tax year.
    /// </summary>
    [JsonIgnore]
    public Dictionary<Asset, Dictionary<int, Quantity>> RealisedGainsPerTaxYear { get; set; }

    /// <summary>
    /// Gets or sets the asset pool state used to compute cost basis and realised gains.
    /// </summary>
    [JsonIgnore]
    public Dictionary<Asset, IAssetPools> AssetPools { get; set; }

    /// <summary>
    /// Gets or sets itemised disposal gain lines by asset.
    /// </summary>
    [JsonIgnore]
    public Dictionary<Asset, List<DisposalGainItem>> DisposalGainItems { get; set; }

    [JsonIgnore]
    public Time InitialTime { get; set; }
    [JsonIgnore]
    public CapitalGains Initial { get; set; }
   
    [JsonIgnore]
    public Dictionary<Time, CapitalGains> HistoricalResults { get; } = new();
}