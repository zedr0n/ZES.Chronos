using System.Collections.Generic;
using Chronos.Core;
using Newtonsoft.Json;
using ZES.Infrastructure.Domain;

namespace Chronos.Accounts.Queries;

[method: JsonConstructor]
public class CapitalGainsQuery(List<string> accounts, Asset denominator, List<Asset> assets = null) : Query<CapitalGains>
{
    /// <summary>
    /// Gets the list of account identifiers included in the capital gains query.
    /// </summary>
    public List<string> Accounts => accounts;

    /// <summary>
    /// Gets the list of assets involved in the capital gains query.
    /// </summary>
    public List<Asset> Assets => assets;
    
    /// <summary>
    /// Gets the asset used as the reporting denominator for values, cashflows, cost basis, and realised gains.
    /// </summary>
    public Asset Denominator => denominator;
    
    /// <summary>
    /// Gets or sets a value indicating whether missing or stale quotes should be refreshed from the configured quote provider.
    /// </summary>
    /// <remarks>
    /// When enabled, account valuation and gains calculations may trigger quote updates while resolving asset values.
    /// </remarks>
    public bool QueryNet { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the cache should be enforced during the execution of the update quote command.
    /// When set to true, cached data will be prioritised and utilized if available; otherwise, the update
    /// operation may fetch fresh data regardless of any existing cached entries.
    /// </summary>
    public bool EnforceCache { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether asset transfers out on the query timestamp should be included in gains calculations.
    /// </summary>
    /// <remarks>
    /// This can be disabled when computing transfer-in cost basis from the source account at the exact transfer time.
    /// </remarks>
    public bool IncludeTransfersOutAtQueryDate { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of days used for matching disposals with future acquisitions in gains calculations.
    /// </summary>
    public int NumberOfMatchingDays { get; set; } = 30;
    
    /// <summary>
    /// Gets or sets operation-scoped asset quote overrides used while valuing account history.
    /// </summary>
    /// <remarks>
    /// Overrides are passed through to asset quote resolution so transaction-specific prices can participate in normal
    /// direct, inverse, or triangulated quote paths.
    /// </remarks>
    public List<AssetQuoteOverride> AssetQuoteOverrides { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether disposal lots should be tracked.
    /// </summary>
    public bool TrackDisposalLots { get; set; }
}

