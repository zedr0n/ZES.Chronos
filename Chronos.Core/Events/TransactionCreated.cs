using Newtonsoft.Json;
using ZES.Infrastructure.Domain;

namespace Chronos.Core.Events;

/// <summary>
/// Represents the creation of a transaction event within the system.
/// This event contains details about a transaction, including its identifier,
/// associated quantity, type, and an optional comment.
/// </summary>
/// <remarks>
/// This class is designed to capture the creation of a transaction and its relevant details.
/// It is typically used in event-sourced systems to record changes and propagate updates.
/// </remarks>
/// <param name="txId">The unique identifier of the transaction.</param>
/// <param name="quantity">The quantity associated with the transaction, including the amount and asset denominator.</param>
/// <param name="transactionType">The type of the transaction. Can be one of the predefined types such as General, Asset, Fee, etc.</param>
/// <param name="comment">An optional string providing additional information or context about the transaction.</param>
/// <param name="assetId">The unique identifier of the asset associated with the transaction.</param>
[method: JsonConstructor]
public class TransactionCreated(string txId, Quantity quantity, Transaction.TransactionType transactionType, string comment, string assetId) : Event
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionCreated"/> class.
    /// </summary>
    public TransactionCreated()
        : this(null, null, Transaction.TransactionType.Unknown, null, null) { }
    
    /// <summary>
    /// Gets the unique identifier of the transaction.
    /// This property represents the transaction ID associated with the event or process.
    /// </summary>
    public string TxId => txId;

    /// <summary>
    /// Gets the quantity associated with the transaction.
    /// This property includes the amount and the asset denominator
    /// that defines the measurement for the quantity.
    /// </summary>
    public Quantity Quantity => quantity;

    /// <summary>
    /// Gets the type of the transaction.
    /// This property specifies the nature of the transaction, which can be one of the predefined types such as General, Asset, Fee, Dividend, Interest, Transfer, or Unknown.
    /// </summary>
    public Transaction.TransactionType TransactionType => transactionType;

    /// <summary>
    /// Gets the comment associated with the transaction.
    /// This property provides additional context or descriptive information
    /// about the transaction, which may include notes or remarks.
    /// </summary>
    public string Comment => comment;

    /// <summary>
    /// Gets the unique identifier of the asset associated with the transaction.
    /// This property represents the identifier used to distinguish one asset from another within the system.
    /// </summary>
    public string AssetId => assetId;
}