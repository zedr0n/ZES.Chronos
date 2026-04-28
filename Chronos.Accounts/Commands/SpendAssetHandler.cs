using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;
using ZES.Interfaces.EventStore;

namespace Chronos.Accounts.Commands;

/// <summary>
/// Handles the execution of the <c>SpendAsset</c> command for an account aggregate.
/// </summary>
/// <remarks>
/// This handler is responsible for performing the necessary logic when an asset
/// is spent from an account. It interacts with the associated account aggregate
/// to deduct the specified asset quantity and cost by invoking the corresponding
/// methods on the aggregate root.
/// </remarks>
public class SpendAssetHandler : CommandHandlerBase<SpendAsset, Account> 
{
    /// <summary>
    /// Handles the SpendAsset command for an account.
    /// </summary>
    /// <remarks>
    /// This handler processes the logic to deduct a specified quantity of an asset and its associated cost
    /// from the target account. It ensures that the appropriate functions in the Account aggregate are
    /// invoked to make necessary updates based on the SpendAsset command.
    /// </remarks>
    public SpendAssetHandler(IEsRepository<IAggregate> repository)
        : base(repository)
    {
    }

    /// <summary>
    /// Executes the action of the <see cref="SpendAsset"/> command on the specified <see cref="Account"/>.
    /// </summary>
    /// <param name="root">The account aggregate on which the command will be applied.</param>
    /// <param name="command">The command specifying the details for spending the asset, including the quantity and cost.</param>
    protected override void Act(Account root, SpendAsset command)
    {
        root.TransactAsset(command.Asset*(-1), command.Cost*(-1), null, false);
    }
}