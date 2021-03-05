using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Chronos.Core;
using Chronos.Core.Json;
using NodaTime;
using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Coins.Commands
{
  /// <inheritdoc />
  public abstract class UpdateDailyMiningHandler : CommandHandlerBase<UpdateDailyMining, Wallet>
  {
    private readonly IEsRepository<IAggregate> _repository;
    private readonly ICommandHandler<RetroactiveCommand<MineCoin>> _mineHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateDailyMiningHandler"/> class.
    /// </summary>
    /// <param name="repository">ES repository</param>
    /// <param name="mineHandler">Wallet balance handler</param>
    protected UpdateDailyMiningHandler(IEsRepository<IAggregate> repository, ICommandHandler<RetroactiveCommand<MineCoin>> mineHandler) 
        : base(repository)
    {
        _repository = repository;
        _mineHandler = mineHandler;
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
        foreach (var v in blocks)
        {
            mineCommand = new RetroactiveCommand<MineCoin>(new MineCoin(command.Address, new Quantity(v.Value.amount, coin.Asset), $"Mining{command.Index}_{idx}"), v.Value.time);
            await _mineHandler.Handle(mineCommand);
            ++idx;
        }

        command.EventType = mineCommand?.EventType;
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
