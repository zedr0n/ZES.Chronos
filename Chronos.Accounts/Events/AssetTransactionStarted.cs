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
/// <param name="queryQuote">A flag indicating whether a quote is required for the transaction.</param>
public class AssetTransactionStarted(Quantity asset, Quantity cost, bool queryQuote) : Event
{
    public AssetTransactionStarted() : this(null, null, false)
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
    /// Gets or sets a value indicating whether to query the quote for the transaction.
    /// </summary>
    /// <remarks>
    /// This property determines if a pricing quote should be retrieved before executing the asset transaction.
    /// It serves as a toggle to enable or disable the quote query process, depending on the requirements of the operation.
    /// </remarks>
    public bool QueryQuote => queryQuote; 
}