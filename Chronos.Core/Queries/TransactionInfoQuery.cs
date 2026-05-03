using Newtonsoft.Json;
using ZES.Infrastructure.Domain;
namespace Chronos.Core.Queries;

/// <summary>
/// Represents a query to retrieve detailed transaction information.
/// </summary>
/// <remarks>
/// The query is designed to provide information about a transaction based on its unique identifier (TxId) and an optional denominator asset.
/// </remarks>
[method: JsonConstructor]
public class TransactionInfoQuery(string txId, Asset denominator = null)
    : SingleQuery<TransactionInfo>(txId)
{
    /// <summary>
    /// Gets the unique identifier of the transaction.
    /// </summary>
    /// <remarks>
    /// The <c>TxId</c> property is a string representation of the transaction's
    /// unique identifier, utilized to retrieve detailed information about the
    /// transaction in related queries or operations.
    /// </remarks>
    public string TxId { get; } = txId;

    /// <summary>
    /// Gets the denominator asset used for currency conversion or representation within the transaction.
    /// </summary>
    /// <remarks>
    /// The <c>Denominator</c> property specifies the asset against which the transaction amount is measured or converted.
    /// It is commonly used to normalize transaction values to a specific currency or unit of measure when performing
    /// operations like querying, reporting, or price lookups.
    /// </remarks>
    public Core.Asset Denominator => denominator;

    /// <summary>
    /// Gets or sets a value indicating whether the transaction amount should be converted
    /// to the specified denominator asset using the exchange rate at the transaction date.
    /// </summary>
    /// <remarks>
    /// When set to <c>true</c>, the conversion to the denominator asset is performed
    /// using the exchange rate as of the transaction's date. If <c>false</c>, the
    /// conversion uses the default behavior, which may rely on a different date or rate
    /// specified elsewhere in the query. This setting is useful for ensuring consistent
    /// historical valuation of transactions in reports or calculations.
    /// </remarks>
    public bool ConvertToDenominatorAtTxDate { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the query should retrieve exchange rate quotes from the internet.
    /// </summary>
    /// <remarks>
    /// The <c>QueryNet</c> property specifies if the query should attempt to fetch external
    /// exchange rate data for the transaction's associated assets. When set to <c>true</c>, the system
    /// will query internet sources for up-to-date quotes based on the transaction's date and assets.
    /// </remarks>
    public bool QueryNet { get; set; }
    
    /// <summary>
    /// Gets or sets a value indicating whether the cache should be enforced during the execution of the update quote command.
    /// When set to true, cached data will be prioritised and utilized if available; otherwise, the update
    /// operation may fetch fresh data regardless of any existing cached entries.
    /// </summary>
    public bool EnforceCache { get; set; }
}