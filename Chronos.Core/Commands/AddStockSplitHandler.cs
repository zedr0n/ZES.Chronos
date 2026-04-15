using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;
using ZES.Interfaces.EventStore;

namespace Chronos.Core.Commands;

/// <summary>
/// Handles the execution of the <see cref="AddStockSplit"/> command for managing stock splits
/// within the <see cref="AssetPair"/> aggregate.
/// </summary>
/// <remarks>
/// This command handler is responsible for applying stock split events to the specified asset pair.
/// The handler updates the aggregate state to reflect the stock split based on the provided command data.
/// </remarks>
public class AddStockSplitHandler : CommandHandlerBase<AddStockSplit, AssetPair>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddStockSplitHandler"/> class.
    /// Handles the execution of the <see cref="AddStockSplit"/> command.
    /// </summary>
    /// <param name="repository">The repository for accessing aggregate roots.</param>
    /// <remarks>
    /// This command handler applies the logic to adjust the stock split for a specified asset pair.
    /// It retrieves the appropriate aggregate root (<see cref="AssetPair"/>) and performs the necessary modifications to reflect the stock split.
    /// </remarks>
    public AddStockSplitHandler(IEsRepository<IAggregate> repository) 
        : base(repository)
    {
    }

    /// <inheritdoc/>
    protected override void Act(AssetPair root, AddStockSplit command)
    {
        root.SplitStock(command.Ratio);
    }
}