using System.Collections.Generic;
using Newtonsoft.Json;
using NodaTime;
using ZES.Interfaces.Domain;

namespace Chronos.Core.Queries;

/// <summary>
/// Represents information about a transaction, encapsulating its details such as transaction ID, date, quantity, type, comments, and associated asset.
/// </summary>
/// <remarks>
/// This class is designed to be immutable and contains essential details to define a transaction's characteristics.
/// It also implements the ISingleState interface to represent a single point of state in a domain-driven design context.
/// </remarks>
[method: JsonConstructor]
public class TransactionInfo(string txId, Instant date, Quantity quantity, Transaction.TransactionType transactionType, string comment, string assetId) : ISingleState
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TransactionInfo"/> class.
    /// </summary>
    public TransactionInfo()
        : this(null, default, null, Transaction.TransactionType.Unknown, null, null)
    { }
    
    /// <summary>
    /// Gets a collection of quotes associated with a transaction.
    /// Each quote in the collection is represented as a unique <see cref="Quantity"/>,
    /// which provides information about the amount and the associated asset denominator.
    /// </summary>
    public HashSet<Quantity> Quotes { get; init; } = new();

    /// <summary>
    /// Gets the unique identifier of the transaction.
    /// </summary>
    /// <remarks>
    /// This property represents a distinct transaction identifier (TxId) used
    /// to track and retrieve specific transaction information.
    /// </remarks>
    public string TxId => txId;

    /// <summary>
    /// Gets the date and time when the transaction occurred.
    /// </summary>
    /// <remarks>
    /// This property represents the point in time associated with the transaction.
    /// It's stored as an instance of <see cref="NodaTime.Instant"/>, which ensures
    /// high-precision timestamp handling and time zone independence.
    /// </remarks>
    public NodaTime.Instant Date => date;

    /// <summary>
    /// Gets the quantity of an asset in a financial transaction.
    /// </summary>
    /// <remarks>
    /// The <c>Quantity</c> consists of two components: the numerical amount and the asset denominator.
    /// It is used to define the specific amount of a given asset involved in the transaction.
    /// </remarks>
    public Quantity Quantity => quantity;

    /// <summary>
    /// Gets the type of transaction within the system.
    /// This property specifies the nature of the transaction by categorizing it into one of the predefined types,
    /// such as General, Asset, Fee, Dividend, Interest, Transfer, or Unknown.
    /// </summary>
    public Transaction.TransactionType TransactionType => transactionType;

    /// <summary>
    /// Gets the comment associated with the transaction.
    /// </summary>
    /// <remarks>
    /// This property provides a textual description or remark
    /// about the transaction. It can be used to store additional
    /// contextual information or notes relevant to the transaction.
    /// </remarks>
    public string Comment => comment;

    /// <summary>
    /// Gets the identifier of the asset associated with the transaction.
    /// </summary>
    /// <remarks>
    /// The AssetId property provides a way to reference the specific asset involved in a transaction.
    /// It is expected to be a string that uniquely identifies the corresponding asset.
    /// </remarks>
    public string AssetId => assetId;
}