namespace Chronos.Core.Commands;

/// <summary>
/// Represents a command to update the details of an existing transaction.
/// </summary>
/// <remarks>
/// This command is used to modify the details of a specific transaction,
/// including its type and associated comment. It targets a transaction
/// identified by a unique transaction ID (txId).
/// </remarks>
public class UpdateTransactionDetails(string txId, Transaction.TransactionType transactionType, string comment) : ZES.Infrastructure.Domain.Command
{
    /// <summary>
    /// Gets the type of the transaction associated with the command.
    /// </summary>
    /// <remarks>
    /// The <c>TransactionType</c> property specifies the category or classification
    /// of the transaction, such as General, Asset, Fee, Dividend, Interest, Transfer, or Unknown.
    /// This is used to provide context and metadata for transaction operations.
    /// </remarks>
    public Transaction.TransactionType TransactionType => transactionType;

    /// <summary>
    /// Gets the comment associated with the transaction.
    /// The comment provides additional context or details about the transaction.
    /// </summary>
    public string Comment => comment;

    /// <summary>
    /// Gets the target identifier associated with the transaction.
    /// </summary>
    public override string Target => txId;
}