using Chronos.Core;
using ZES.Infrastructure.Domain;

namespace Chronos.Accounts.Events;

/// <summary>
/// Represents an event that signals the initiation of an asset transaction.
/// This event is used within the domain event system to relay information about
/// the asset being transacted, its associated cost, and whether a quote needs to be queried.
/// </summary>
/// <param name="asset">The amount and type of the asset being transacted.</param>
/// <param name="cost">The monetary or asset value corresponding to the transaction cost.</param>
public class AssetTransactionStarted(Quantity asset, Quantity cost) : Event
{
    public AssetTransactionStarted() : this(null, null)
    {
    }

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
    /// Represents the additional cost or surcharge applied to the transaction.
    /// This value encapsulates the monetary or asset-based fee required to process the transaction.
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
}