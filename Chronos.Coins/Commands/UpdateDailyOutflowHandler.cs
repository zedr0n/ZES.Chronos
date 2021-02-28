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
    /// <summary>
    /// Daily transaction extraction handler
    /// </summary>
    public abstract class UpdateDailyOutflowHandler : CommandHandlerBase<UpdateDailyOutflow, Wallet>
    {
        private readonly ICommandHandler<RetroactiveCommand<ChangeWalletBalance>> _balanceHandler;
        private readonly IEsRepository<IAggregate> _repository;

        /// <summary>
        /// Initializes a new instance of the <see cref="UpdateDailyOutflowHandler"/> class.
        /// </summary>
        /// <param name="repository">Aggregate repository</param>
        /// <param name="balanceHandler">Change balance handler</param>
        public UpdateDailyOutflowHandler(IEsRepository<IAggregate> repository, ICommandHandler<RetroactiveCommand<ChangeWalletBalance>> balanceHandler)
          : base(repository)
        {
          _repository = repository;
          _balanceHandler = balanceHandler;
        }

        /// <inheritdoc/>
        public override async Task Handle(UpdateDailyOutflow command)
        {
          var root = await _repository.Find<Wallet>(command.Target);
          if (root == null)
            throw new ArgumentNullException(nameof(command.Address));

          var coin = await _repository.Find<Coin>(root.Coin);
          if (coin == null)
            throw new ArgumentNullException(nameof(root.Coin));

          var asset = new Asset(coin.Name, coin.Ticker, Asset.Type.Coin); 
          
          var txResults = await GetTransactions(command);
          
          var outflows = new Dictionary<Instant, (Instant time, double amount)>();
          foreach (var tx in txResults)
          {
            var dateTime = Instant.FromUnixTimeMilliseconds(tx.ReceiveTime).InUtc().LocalDateTime;
            var date = new LocalDate(dateTime.Year, dateTime.Month, dateTime.Day);
            var instant = date.AtMidnight().InUtc().ToInstant();
            var amount = tx.Amount;
            if (tx.To == command.Target)
              amount *= -1;
            
            if (outflows.ContainsKey(instant))
              amount += outflows[instant].amount;
            outflows[instant] = (Instant.FromUnixTimeMilliseconds(tx.ReceiveTime), amount);
          }

          ICommand changeBalanceCommand = null;
          var idx = 0;
          foreach (var v in outflows)
          {
            changeBalanceCommand = new RetroactiveCommand<ChangeWalletBalance>(new ChangeWalletBalance(command.Address, new Quantity(-v.Value.amount, asset), $"Out{command.Index}_{idx}"), v.Value.time);
            await _balanceHandler.Handle(changeBalanceCommand);
            ++idx;
          }

          command.EventType = changeBalanceCommand?.EventType;
        }

        /// <summary>
        /// Parse JSON list of transactions from API
        /// </summary>
        /// <param name="command">Input command</param>
        /// <returns>List of transactions</returns>
        protected abstract Task<IEnumerable<Tx>> GetTransactions(UpdateDailyOutflow command);

        /// <inheritdoc/>
        protected override void Act (Wallet wallet, UpdateDailyOutflow command)
        {
          throw new NotImplementedException();
        }
    }
}
