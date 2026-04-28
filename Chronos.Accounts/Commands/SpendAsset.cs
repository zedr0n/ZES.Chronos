using Chronos.Core;
using ZES.Infrastructure.Domain;

namespace Chronos.Accounts.Commands;

/// <summary>
/// Represents a command for spending an asset from a specified account.
/// </summary>
/// <remarks>
/// This command is used to deduct a specified quantity of an asset
/// along with an associated cost from a target account.
/// </remarks>
/// <param name="account">The unique identifier of the account where the asset will be spent.</param>
/// <param name="asset">The quantity of the asset being spent (amount and associated asset type).</param>
/// <param name="cost">The cost associated with the expenditure.</param>
public class SpendAsset(string account, Quantity asset, Quantity cost) : Command
{
   /// <summary>
   /// Gets the quantity of the asset being spent in the operation.
   /// </summary>
   public Quantity Asset => asset;

   /// <summary>
   /// Gets the cost associated with spending an asset.
   /// The cost represents the quantity of a resource or currency required to execute the asset transaction.
   /// </summary>
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