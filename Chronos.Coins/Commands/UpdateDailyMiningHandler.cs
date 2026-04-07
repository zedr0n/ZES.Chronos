using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Chronos.Core;
using Chronos.Core.Json;
using NodaTime;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.Utils;
using ZES.Interfaces.Domain;
using ZES.Interfaces.EventStore;
using ZES.Interfaces.Infrastructure;

namespace Chronos.Coins.Commands
{
  /// <inheritdoc />
  public abstract class UpdateDailyMiningHandler : CommandHandlerBase<UpdateDailyMining, Wallet>
  {
    private readonly IEsRepository<IAggregate> _repository;
    private readonly ICommandHandler<RetroactiveCommand<MineCoin>> _mineHandler;
    private readonly IFlowCompletionService _flowCompletionService;

    /// <summary>
    /// Abstract base class for handling the "Update Daily Mining" command.
    /// </summary>
    /// <remarks>
    /// This handler processes daily mining updates, interacts with the wallet aggregate,
    /// and executes retroactive commands to mine coins.
    /// Specific implementations of this class define platform-specific behavior related to fetching mined blocks.
    /// </remarks>
    /// <param name="repository">Event store repository for aggregate management.</param>
    /// <param name="mineHandler">Handler to execute retroactive mining commands.</param>
    /// <param name="flowCompletionService">Service to manage the completion of workflows.</param>
    protected UpdateDailyMiningHandler(IEsRepository<IAggregate> repository,
        ICommandHandler<RetroactiveCommand<MineCoin>> mineHandler, IFlowCompletionService flowCompletionService)
        : base(repository)
    {
        _repository = repository;
        _mineHandler = mineHandler;
        _flowCompletionService = flowCompletionService;
    }

    /// <inheritdoc/>
    public override async Task Handle(UpdateDailyMining command)
    {
        var root = await _repository.Find<Wallet>(command.Target);
        if (root == null)
            throw new ArgumentNullException(nameof(command.Address));

        var coin = await _repository.Find<Coin>(root.Coin);
        if (coin == null)
            throw new ArgumentNullException(nameof(root.Coin));

        var minedBlocks = await GetMinedBlocks(command);
        if (minedBlocks == null)
            return;
        
        var blocks = new Dictionary<Instant, (Instant time, double amount)>();
        foreach (var block in minedBlocks)
        {
            var dateTime = Instant.FromUnixTimeMilliseconds(block.Timestamp).InUtc().LocalDateTime;
            var date = new LocalDate(dateTime.Year, dateTime.Month, dateTime.Day);
            var instant = date.AtMidnight().InUtc().ToInstant();
            var amount = block.Feereward;
            if (blocks.ContainsKey(instant))
                amount += blocks[instant].amount;
            blocks[instant] = (Instant.FromUnixTimeMilliseconds(block.Timestamp), amount);
        }
        
        ICommand mineCommand = null;
        var idx = 0;
        _flowCompletionService.SetIgnore(command, true);
        foreach (var v in blocks)
        {
            mineCommand = new RetroactiveCommand<MineCoin>(new MineCoin(command.Address, new Quantity(v.Value.amount, coin.Asset), $"Mining{command.Index}_{idx}"), v.Value.time.ToTime());
            await _mineHandler.Handle(mineCommand);
            ++idx;
        }
        _flowCompletionService.SetIgnore(command, false);
    }

    /// <inheritdoc/>
    protected override void Act (Wallet wallet, UpdateDailyMining command)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Gets the list mined blocks from server
    /// </summary>
    /// <param name="command">Input command</param>
    /// <returns>List of mined blocks</returns>
    protected abstract Task<IEnumerable<MinedBlock>> GetMinedBlocks(UpdateDailyMining command);
  }
}
