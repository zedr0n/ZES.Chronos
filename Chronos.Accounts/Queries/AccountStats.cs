using System.Collections.Generic;
using Chronos.Core;
using Newtonsoft.Json;
using NodaTime;

namespace Chronos.Accounts.Queries;

/// <summary>
/// Represents account valuation, position, income, and realised-gain statistics for a single account or account group.
/// </summary>
/// <param name="balance">The total account balance expressed in the query denominator.</param>
[method: JsonConstructor]
public class AccountStats(Quantity balance)
{
    /// <summary>
    /// Gets the total account balance expressed in the query denominator.
    /// </summary>
    public Quantity Balance => balance;
    
    /// <summary>
    /// Gets or sets the held asset quantities by asset.
    /// </summary>
    public List<Quantity> Positions { get; set; }

    /// <summary>
    /// Gets or sets the current market values of the held positions, expressed in the query denominator.
    /// </summary>
    public List<Quantity> Values { get; set; }

    /// <summary>
    /// Gets or sets dividend totals by asset, expressed in the query denominator.
    /// </summary>
    public List<Quantity> Dividends { get; set; }

    /// <summary>
    /// Gets or sets remaining cost basis by asset, expressed in the query denominator.
    /// </summary>
    public List<Quantity> CostBasis { get; set; }

    /// <summary>
    /// Gets or sets realised gains by asset, expressed in the query denominator.
    /// </summary>
    public List<Quantity> RealisedGains { get; set; }

    /// <summary>
    /// Gets or sets the cash balance after subtracting position values from the total balance.
    /// </summary>
    public Quantity CashBalance { get; set; }

    /// <summary>
    /// Gets or sets the total dividend amount across all assets, expressed in the query denominator.
    /// </summary>
    public Quantity TotalDividend { get; set; }
    
    /// <summary>
    /// Gets or sets realised gains grouped by asset and tax year.
    /// </summary>
    [JsonIgnore]
    public Dictionary<Asset, Dictionary<int, Quantity>> RealisedGainsPerTaxYear { get; set; }

    /// <summary>
    /// Gets or sets external cashflows used for internal rate of return calculation.
    /// </summary>
    [JsonIgnore]
    public List<(Instant, Quantity)> ExternalCashflows { get; set; }

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

    /// <summary>
    /// Gets or sets the underlying account statistics state used to produce this result.
    /// </summary>
    [JsonIgnore]
    public AccountStatsState State { get; set; }

    /// <summary>
    /// Gets or sets the internal rate of return for the account.
    /// </summary>
    public double Irr { get; set; }
}
