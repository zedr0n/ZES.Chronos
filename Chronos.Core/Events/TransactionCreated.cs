using Newtonsoft.Json;
using ZES.Infrastructure.Domain;

namespace Chronos.Core.Events;

/// <summary>
/// Represents an event indicating the creation of a transaction in the system.
/// This event encapsulates key details about the transaction, such as the identifier,
/// quantity, transaction type, and optional metadata.
/// </summary>
/// <remarks>
/// This class is intended to record transaction creation events and provide detailed contextual
/// information for event-sourced architectures or transactional systems.
/// It includes properties to describe the transaction, its nature, and associated entities.
/// </remarks>
/// <param name="txId">The unique identifier of the transaction.</param>
/// <param name="quantity">The quantity involved in the transaction, including the value and unit of measurement.</param>
/// <param name="transactionType">The type of transaction being created, such as General, Asset, Fee, etc.</param>
/// <param name="comment">Optional contextual information or description related to the transaction.</param>
/// <param name="assetId">The unique identifier of the asset related to the transaction.</param>
/// <param name="counterpartyAccountId">The originating account identifier for the transaction, typically used in contextual transfers.</param>
[method: JsonConstructor]
public class TransactionCreated(
    string txId,
    Quantity quantity,
    Transaction.TransactionType transactionType,
    string comment,
    string assetId,
    string counterpartyAccountId) : Event
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionCreated"/> class.
    /// </summary>
    public TransactionCreated()
        : this(null, null, Transaction.TransactionType.Unknown, null, null, null)
    {
    }

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

    /// <summary>
    /// Gets the account identifier of the counterparty involved in the transaction.
    /// This property represents the unique identifier for the account associated with the counterparty.
    /// </summary>
    public string CounterpartyAccountId => counterpartyAccountId;
}