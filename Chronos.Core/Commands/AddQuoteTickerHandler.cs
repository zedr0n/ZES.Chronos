using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;
using ZES.Interfaces.EventStore;

namespace Chronos.Core.Commands;

/// <summary>
/// Handles the execution of the <see cref="AddQuoteTicker"/> command for the <see cref="AssetPair"/> aggregate.
/// </summary>
/// <remarks>
/// This command handler is responsible for associating a ticker with an asset pair.
/// It utilizes the <see cref="AssetPair.AddTicker(string)"/> method to perform the required operation on the aggregate.
/// </remarks>
public class AddQuoteTickerHandler : CommandHandlerBase<AddQuoteTicker, AssetPair>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AddQuoteTickerHandler"/> class.
    /// Handles the execution of the <see cref="AddQuoteTicker"/> command for the <see cref="AssetPair"/> aggregate.
    /// </summary>
    /// <param name="repository">The repository for accessing aggregate roots.</param>
    /// <remarks>
    /// This command handler is responsible for associating a ticker with an asset pair
    /// by utilizing the <see cref="AssetPair.AddTicker(string)"/> method.
    /// </remarks>
    public AddQuoteTickerHandler(IEsRepository<IAggregate> repository)
        : base(repository)
    {
    }

    /// <inheritdoc/>
    protected override void Act(AssetPair root, AddQuoteTicker command)
    {
        root.AddTicker(command.Ticker);
    }
}