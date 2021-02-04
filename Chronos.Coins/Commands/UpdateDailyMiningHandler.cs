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
    private readonly ICommandHandler<RetroactiveCommand<ChangeWalletBalance>> _balanceHandler;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateDailyMiningHandler"/> class.
    /// </summary>
    /// <param name="repository">ES repository</param>
    /// <param name="balanceHandler">Wallet balance handler</param>
    protected UpdateDailyMiningHandler(IEsRepository<IAggregate> repository, ICommandHandler<RetroactiveCommand<ChangeWalletBalance>> balanceHandler) 
        : base(repository)
    {
        _repository = repository;
        _balanceHandler = balanceHandler;
    }
    
    /// <summary>
    /// Gets daily mining asset
    /// </summary>
    protected abstract Asset Asset { get; }

    /// <inheritdoc/>
    public override async Task Handle(UpdateDailyMining command)
    {
        var root = await _repository.Find<Wallet>(command.Target);
        if (root == null)
            throw new ArgumentNullException(nameof(command.Address));

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
        
        ICommand changeBalanceCommand = null;
        var idx = 0;
        foreach (var v in blocks)
        {
            changeBalanceCommand = new RetroactiveCommand<ChangeWalletBalance>(new ChangeWalletBalance(command.Address, new Quantity(v.Value.amount, Asset), $"Mining{command.Index}_{idx}"), v.Value.time);
            await _balanceHandler.Handle(changeBalanceCommand);
            ++idx;
        }

        command.EventType = changeBalanceCommand?.EventType;
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
