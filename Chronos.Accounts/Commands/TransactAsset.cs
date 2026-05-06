using System.Text.Json.Serialization;
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
/// <param name="account">The unique identifier of the account where the asset transaction is applied.</param>
/// <param name="asset">The quantity of the asset being acquired or disposed.</param>
/// <param name="cost">
/// The consideration associated with the asset transaction. Use <see cref="double.NaN"/> as the amount
/// to request quote-based market valuation, zero for a zero-cost acquisition or zero-proceeds disposal,
/// or an explicit amount when the transaction consideration is known.
/// </param>
[method: JsonConstructor]
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
    /// Represents the transaction consideration, specified in the associated asset denominator.
    /// <see cref="double.NaN"/> requests quote-based market valuation, zero records zero consideration,
    /// and an explicit amount records known consideration.
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

    /// <summary>
    /// Gets or sets a value indicating whether an offsetting cost transaction should be created.
    /// </summary>
    /// <remarks>
    /// When set to <c>true</c>, an additional transaction is initiated to offset the cost
    /// associated with the primary asset transaction. If <c>false</c>, no offsetting transaction
    /// is created, and only the primary transaction is executed. This property is useful for ensuring
    /// accurate cost allocation and maintaining proper financial reporting within the system.
    /// </remarks>
    public bool CreateOffsettingCostTransaction { get; set; } = true;
    
    public override string Target => account;
}
