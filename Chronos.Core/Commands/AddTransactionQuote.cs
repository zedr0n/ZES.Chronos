using Newtonsoft.Json;

namespace Chronos.Core.Commands;

/// <summary>
/// Represents a command to add a transaction quote to an existing transaction.
/// </summary>
/// <remarks>
/// This command is used to associate a specified quantity of an asset
/// with an existing transaction identified by its transaction ID.
/// </remarks>
/// <param name="txId">The unique identifier of the target transaction.</param>
/// <param name="quantity">The quoted quantity of the asset to be associated with the transaction.</param>
[method: JsonConstructor]
public class AddTransactionQuote(string txId, Quantity quantity) : ZES.Infrastructure.Domain.Command
{
    /// <summary>
    /// Gets the quantity of an asset in a financial or transaction context.
    /// </summary>
    /// <remarks>
    /// The Quantity defines a measurable amount of an asset paired with an associated denominator
    /// (e.g., currency or other asset type). It is used to specify asset amounts in various operations
    /// such as transactions and asset management.
    /// </remarks>
    public Quantity Quantity => quantity;
    
    /// <inheritdoc/>
    public override string Target => txId;
}