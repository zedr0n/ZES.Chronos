using Chronos.Core;
using ZES.Infrastructure.Domain;

namespace Chronos.Accounts.Commands;

/// <summary>
/// Represents a command to transact an asset by specifying an account, amount, and cost.
/// </summary>
/// <remarks>
/// This class is used to execute an asset transaction within an account. The transaction involves
/// a specified asset amount and the associated cost for the operation.
/// </remarks>
public class TransactAsset(string account, Quantity asset, Quantity cost) : Command
{
    /// <summary>
    /// Gets the amount of the asset involved in the transaction.
    /// </summary>
    /// <remarks>
    /// Represents the quantity of the asset to be transacted.
    /// This value is defined at the creation of the transaction command
    /// and encapsulates both the numeric magnitude and its associated asset denominator.
    /// </remarks>
    public Quantity Asset => asset;

    /// <summary>
    /// Gets the cost associated with the asset transaction.
    /// </summary>
    /// <remarks>
    /// Represents the value required to complete the transaction, specified in the associated asset denominator.
    /// This property is determined at the creation of the command and works in tandem with the asset amount.
    /// </remarks>
    public Quantity Cost => cost;

    /// <summary>
    /// Gets or sets the fee associated with the asset transaction.
    /// </summary>
    /// <remarks>
    /// Represents the cost or charge incurred as part of processing the transaction.
    /// This value is expressed as a <see cref="Quantity"/> object, which includes the fee amount
    /// and its corresponding asset denominator, such as a currency or other value unit.
    /// </remarks>
    public Quantity Fee { get; set; }
    
    public override string Target => account;
}