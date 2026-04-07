using Chronos.Core;
using ZES.Infrastructure.Domain;

namespace Chronos.Accounts.Events;

/// <summary>
/// Represents an event indicating that an asset transaction has been initiated.
/// Used as part of the domain events system to communicate the start of a transaction,
/// carrying details about the quantity of assets involved in the transaction and
/// associated costs.
/// </summary>
/// <param name="asset">The quantity of the asset being transacted.</param>
/// <param name="cost">The cost associated with the transaction in terms of another asset or currency.</param>
public class AssetTransactionStarted(Quantity asset, Quantity cost) : Event
{
    public AssetTransactionStarted() : this(null, null) {}
    
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
}