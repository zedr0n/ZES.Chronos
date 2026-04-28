using Chronos.Core;
using ZES.Infrastructure.Domain;

namespace Chronos.Accounts.Commands;

/// <summary>
/// Represents a command for spending an asset from a specified account.
/// </summary>
/// <remarks>
/// This command is used to deduct a specified quantity of an asset from a target account without
/// creating an offsetting cash transaction. The associated cost is still recorded for cost-basis
/// and realised-gain calculations.
/// </remarks>
/// <param name="account">The unique identifier of the account where the asset will be spent.</param>
/// <param name="asset">The quantity of the asset being spent (amount and associated asset type).</param>
/// <param name="cost">
/// The consideration associated with the asset disposal. Use <see cref="double.NaN"/> as the amount
/// to request quote-based market valuation, zero for a zero-proceeds disposal, or an explicit amount
/// when the disposal consideration is known.
/// </param>
public class SpendAsset(string account, Quantity asset, Quantity cost) : Command
{
   /// <summary>
   /// Gets the quantity of the asset being spent in the operation.
   /// </summary>
   public Quantity Asset => asset;

   /// <summary>
   /// Gets the consideration associated with spending the asset.
   /// </summary>
   /// <remarks>
   /// <see cref="double.NaN"/> requests quote-based market valuation, zero records a zero-proceeds
   /// disposal, and an explicit amount records known disposal consideration. No offsetting cash
   /// transaction is created by this command.
   /// </remarks>
   public Quantity Cost => cost;

   /// <summary>
   /// Gets the target account to which the asset spending operation applies.
   /// </summary>
   /// <remarks>
   /// The target represents the account identifier associated with the command.
   /// It specifies the context in which the operation is executed.
   /// </remarks>
   public override string Target => account; 
}
