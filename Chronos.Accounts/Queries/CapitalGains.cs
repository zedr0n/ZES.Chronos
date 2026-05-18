using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;
using Chronos.Core;
using ZES.Interfaces.Clocks;
using ZES.Interfaces.Domain;

namespace Chronos.Accounts.Queries;

public class CapitalGains : IHistoricalState, IHistoricalResults<CapitalGains>
{
    public CapitalGains() { }

    public CapitalGains(CapitalGains other)
    {
        Assets = other.Assets?.ToList(); 
        RealisedGains = new Dictionary<Asset, Quantity>(other.RealisedGains);
        CostBasis = new Dictionary<Asset, Quantity>(other.CostBasis);
        RealisedGainsPerTaxYear = other.RealisedGainsPerTaxYear.ToDictionary(x => x.Key, x => new Dictionary<int, Quantity>(x.Value));
        AssetPools = other.AssetPools.ToDictionary(x => x.Key, x => x.Value.Copy()); 
        DisposalGainItems = other.DisposalGainItems.ToDictionary(x => x.Key, x => x.Value.ToList()); 
    }

    public List<string> Diff(CapitalGains other)
    {
        var differences = new List<string>();

        //CompareAssetList(differences, nameof(Assets), Assets, other.Assets);
        CompareQuantityDictionary(differences, nameof(RealisedGains), RealisedGains, other.RealisedGains);
        CompareQuantityDictionary(differences, nameof(CostBasis), CostBasis, other.CostBasis);
        //CompareTaxYearDictionary(differences, nameof(RealisedGainsPerTaxYear), RealisedGainsPerTaxYear, other.RealisedGainsPerTaxYear);
        ComparePoolDictionary(differences, nameof(AssetPools), AssetPools, other.AssetPools);
        //CompareDisposalItems(differences, nameof(DisposalGainItems), DisposalGainItems, other.DisposalGainItems);

        return differences;
    }
    
    public CapitalGains Copy() => new(this);
    
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
    public Dictionary<Time, CapitalGains> HistoricalResults { get; } = new();
    
    private static void CompareQuantityDictionary(
        List<string> differences,
        string name,
        Dictionary<Asset, Quantity> left,
        Dictionary<Asset, Quantity> right,
        double tolerance = 1e-8)
    {
        foreach (var asset in left.Keys.Union(right.Keys))
        {
            if (!left.TryGetValue(asset, out var l))
            {
                differences.Add($"{name}[{asset}] missing on left");
                continue;
            }

            if (!right.TryGetValue(asset, out var r))
            {
                differences.Add($"{name}[{asset}] missing on right");
                continue;
            }

            if (l == null || r == null)
            {
                if (l != r)
                    differences.Add($"{name}[{asset}] null mismatch: {l} vs {r}");
                continue;
            }

            if (l.Denominator != r.Denominator || Math.Abs(l.Amount - r.Amount) > tolerance)
                differences.Add($"{name}[{asset}] differs: {l} vs {r}");
        }
    }
    
    private static void ComparePoolDictionary(
        List<string> differences,
        string name,
        Dictionary<Asset, IAssetPools> left,
        Dictionary<Asset, IAssetPools> right,
        double tolerance = 1e-8)
    {
        foreach (var asset in left.Keys.Union(right.Keys))
        {
            if (!left.TryGetValue(asset, out var l))
            {
                differences.Add($"{name}[{asset}] missing on left");
                continue;
            }

            if (!right.TryGetValue(asset, out var r))
            {
                differences.Add($"{name}[{asset}] missing on right");
                continue;
            }

            if (Math.Abs(l.TotalQuantity - r.TotalQuantity) > tolerance)
                differences.Add($"{name}[{asset}].TotalQuantity differs: {l.TotalQuantity} vs {r.TotalQuantity}");

            if (Math.Abs(l.CostBasis - r.CostBasis) > tolerance)
                differences.Add($"{name}[{asset}].CostBasis differs: {l.CostBasis} vs {r.CostBasis}");

            if (Math.Abs(l.RealisedGain - r.RealisedGain) > tolerance)
                differences.Add($"{name}[{asset}].RealisedGain differs: {l.RealisedGain} vs {r.RealisedGain}");
        }
    }
}