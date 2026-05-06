using Newtonsoft.Json;
using ZES.Infrastructure.Domain;

namespace Chronos.Accounts.Queries;

/// <summary>
/// Represents a query to retrieve a list of transactions for a specific account or entity
/// identified by its name.
/// </summary>
[method: JsonConstructor]
public class TransactionListQuery(string name) : SingleQuery<TransactionList>(name)
{
    /// <summary>
    /// Gets the name associated with the transaction list query.
    /// </summary>
    /// <remarks>
    /// The Name property represents a unique identifier or key used to specify the transaction list
    /// when executing the query. It is initialized through the constructor and is immutable.
    /// </remarks>
    public string Name { get; } = name;

    /// <summary>
    /// Gets or sets a value indicating whether detailed information about transactions
    /// should be included in the query results.
    /// </summary>
    public bool IncludeInfo { get; set; } = true;
}