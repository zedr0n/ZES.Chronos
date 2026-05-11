using Chronos.Core;
using ZES.Infrastructure.Domain;

namespace Chronos.Accounts.Commands;

/// <summary>
/// Represents a command for receiving an asset into a specified account.
/// </summary>
/// <remarks>
/// This command is used to add a specified quantity of an asset to a target account without creating
/// an offsetting cash transaction. The associated cost is still recorded for cost-basis and realised-gain
/// calculations.
/// </remarks>
/// <param name="account">The unique identifier of the account where the asset will be received.</param>
/// <param name="asset">The quantity of the asset being received (amount and associated asset type).</param>
/// <param name="cost">
/// The cost associated with the acquisition of the asset. Use <see cref="double.NaN"/> as the amount to request
/// quote-based market valuation, zero for a no-cost acquisition, or an explicit amount when the acquisition cost
/// is known.
/// </param>
public class ReceiveAsset(string account, Quantity asset, Quantity cost) : Command
{
    /// <summary>
    /// Gets the details of the asset, including its unique identifier and type.
    /// </summary>
    public Quantity Asset => asset;

    /// <summary>
    /// Gets or sets the cost associated with the acquisition of the asset.
    /// The cost is used for cost-basis and realized-gain calculations.
    /// A value of <see cref="double.NaN"/> requests quote-based market valuation,
    /// zero indicates a no-cost acquisition, and an explicit value is used when the acquisition cost is known.
    /// </summary>
    public Quantity Cost => cost;

    /// <summary>
    /// Gets the unique identifier of the target account associated with the received asset.
    /// </summary>
    public override string Target => account;
}