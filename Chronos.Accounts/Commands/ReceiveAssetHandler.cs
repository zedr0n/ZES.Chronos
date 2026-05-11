using Chronos.Accounts.Events;
using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;
using ZES.Interfaces.EventStore;

namespace Chronos.Accounts.Commands;

/// <summary>
/// Handles the execution of the <c>ReceiveAsset</c> command for an account aggregate.
/// </summary>
/// <remarks>
/// This handler is responsible for processing the logic to record the receipt of an asset
/// within an account. It interacts with the associated account aggregate to add the specified
/// asset quantity and its cost by invoking the corresponding methods on the aggregate root.
/// </remarks>
public class ReceiveAssetHandler : CommandHandlerBase<ReceiveAsset, Account>
{
    /// <summary>
    /// Handles the execution of the <c>ReceiveAsset</c> command for an account aggregate.
    /// </summary>
    /// <remarks>
    /// This handler is responsible for processing the logic to record the receipt of an asset
    /// within an account. It interacts with the associated account aggregate to add the specified
    /// asset quantity and its cost by invoking the corresponding methods on the aggregate root.
    /// </remarks>
    public ReceiveAssetHandler(IEsRepository<IAggregate> repository)
        : base(repository)
    {
    }

    /// <summary>
    /// Executes the handling logic for the <see cref="ReceiveAsset"/> command on the specified <see cref="Account"/>.
    /// </summary>
    /// <param name="root">The account aggregate on which the command will be applied.</param>
    /// <param name="command">The command that contains details for receiving an asset, including its quantity and associated cost.</param>
    protected override void Act(Account root, ReceiveAsset command)
    {
        root.TransactAsset(command.Asset, command.Cost, null, false, assetTransactionType: AssetTransactionType.Income);
    }
}