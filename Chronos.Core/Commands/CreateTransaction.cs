using Newtonsoft.Json;
using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Core.Commands;

/// <summary>
/// Represents a command to create a transaction in the system.
/// </summary>
/// <remarks>
/// This command is used to initiate a new transaction with specific details such as the transaction ID, amount, type,
/// and an optional comment. Transactions created using this command can be associated with accounts and further processed within the system.
/// </remarks>
[method: JsonConstructor]
public class CreateTransaction(string txId, Quantity amount, Transaction.TransactionType transactionType, string comment, string assetId = null) : Command, ICreateCommand
{
    /// <summary>
    /// Gets the unique transaction identifier associated with the command.
    /// This identifier is used to uniquely reference a specific transaction
    /// within the domain.
    /// </summary>
    public string TxId => txId;

    /// <summary>
    /// Gets the quantity amount associated with the transaction.
    /// The amount represents the numerical value and corresponding asset
    /// that are part of the transaction details.
    /// </summary>
    public Quantity Amount => amount;

    /// <summary>
    /// Gets the type of the transaction associated with the command.
    /// This indicates the nature of the transaction, such as whether it is
    /// a general transaction, asset-related, fee, dividend, interest, transfer, or unknown type.
    /// </summary>
    public Transaction.TransactionType TransactionType => transactionType;

    /// <summary>
    /// Gets the optional comment associated with the transaction being created.
    /// The comment can provide additional context or details about the transaction.
    /// </summary>
    public string Comment => comment;

    /// <summary>
    /// Gets the target identifier associated with the command, typically used to identify
    /// the aggregate root instance the command is intended to act upon.
    /// </summary>
    public override string Target => TxId;

    /// <summary>
    /// Gets the unique identifier of the asset associated with the transaction.
    /// This identifier is used to reference a specific asset within the system,
    /// enabling accurate tracking and association with transactions.
    /// </summary>
    public string AssetId => assetId; 
}