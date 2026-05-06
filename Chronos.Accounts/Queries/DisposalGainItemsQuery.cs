using System.Collections.Generic;
using Chronos.Core;
using Newtonsoft.Json;
using ZES.Infrastructure.Domain;

namespace Chronos.Accounts.Queries;

[method: JsonConstructor]
public class DisposalGainItemsQuery(List<string> accounts, Asset asset, Asset denominator) : Query<DisposalGainItems>
{
    /// <summary>
    /// Gets the account name to query.
    /// </summary>
    public List<string> Accounts => accounts;

    /// <summary>
    /// Gets the asset used as the reporting denominator for values, cashflows, cost basis, and realised gains.
    /// </summary>
    public Asset Denominator => denominator;

    /// <summary>
    /// Gets the asset associated with the query.
    /// </summary>
    public Asset Asset => asset;
    
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
    /// Gets or sets operation-scoped asset quote overrides used while valuing account history.
    /// </summary>
    /// <remarks>
    /// Overrides are passed through to asset quote resolution so transaction-specific prices can participate in normal
    /// direct, inverse, or triangulated quote paths.
    /// </remarks>
    public List<AssetQuoteOverride> AssetQuoteOverrides { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the disposal gains should be aggregated.
    /// </summary>
    public bool AggregateDisposalGains { get; set; } = true;
}