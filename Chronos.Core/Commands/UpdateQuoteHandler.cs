/// <filename>
///     UpdateQuoteHandler.cs
/// </filename>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ZES.Infrastructure;
using ZES.Infrastructure.Alerts;
using ZES.Infrastructure.Net;
using ZES.Infrastructure.Utils;
using ZES.Interfaces.Domain;
using ZES.Interfaces.Net;
using ZES.Interfaces.Pipes;

namespace Chronos.Core.Commands
{
  /// <inheritdoc />
  public class UpdateQuoteHandler : ZES.Infrastructure.Domain.CommandHandlerBase<UpdateQuote, AssetPair>
  {
    private readonly IEsRepository<IAggregate> _repository;
    private readonly ICollection<ICommandHandler<UpdateQuote>> _handlers;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateQuoteHandler"/> class.
    /// </summary>
    /// <param name="repository">Aggregate repository</param>
    /// <param name="handlers">Quote handlers</param>
    public UpdateQuoteHandler(IEsRepository<IAggregate> repository, ICollection<ICommandHandler<UpdateQuote>> handlers) 
    : base(repository)
    {
      _repository = repository;
      _handlers = handlers;
    }

    /// <inheritdoc/>
    public override async Task Handle(UpdateQuote command)
    {
      var root = await _repository.Find<AssetPair>(command.Target);
      if (root == null)
        throw new ArgumentNullException(nameof(AssetPair));

      if (root.QuoteDates.Any(d =>
        d.InUtc().Year == command.Timestamp.ToInstant().InUtc().Year && d.InUtc().Month == command.Timestamp.ToInstant().InUtc().Month &&
        d.InUtc().Day == command.Timestamp.ToInstant().InUtc().Day))
      {
        throw new InvalidOperationException(
          $"Quote already added for {command.Timestamp.ToInstant().InUtc().ToString("yyyy-MM-dd", new DateTimeFormatInfo())}");
      }

      ICommandHandler handler = null;
      ICommand commandT = null;
      if (root.ForAsset.AssetType == AssetType.Currency && root.DomAsset.AssetType == AssetType.Currency)
      {
        commandT = new UpdateQuote<Api.Fx.JsonResult>(command.Target);
        handler = _handlers.SingleOrDefault(h => h.CanHandle(commandT));
      }
      else if (root.ForAsset.AssetType == AssetType.Coin && root.DomAsset.AssetType == AssetType.Currency)
      {
        commandT = new UpdateQuote<Api.Coin.JsonResult>(command.Target);
        handler = _handlers.SingleOrDefault(h => h.CanHandle(commandT));
      }
      
      if (handler == null)
        throw new InvalidOperationException($"Automatic quote retrieval for {root.ForAsset.Ticker}{root.DomAsset.Ticker} not supported");
      
      await handler.Handle(commandT);
      command.EventType = commandT.EventType;
    }

    /// <inheritdoc/>
    protected override void Act(AssetPair root, UpdateQuote command)
    {
      throw new NotImplementedException();
    }
  }

  /// <inheritdoc cref="ZES.Interfaces.Domain.ICommandHandler" />
  public class UpdateQuoteHandler<T> : ZES.Infrastructure.Domain.CommandHandlerBase<UpdateQuote<T>, AssetPair>, ICommandHandler<UpdateQuote>
    where T : class, IJsonResult
  {
    private readonly ICommandHandler<AddQuote> _handler;
    private readonly ICommandHandler<RequestJson<T>> _jsonRequestHandler;
    private readonly IEsRepository<IAggregate> _repository;
    private readonly IMessageQueue _messageQueue;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateQuoteHandler{T}"/> class.
    /// </summary>
    /// <param name="repository">Aggregate repository</param>
    /// <param name="handler">Quote handler</param>
    /// <param name="jsonRequestHandler">JSON handler</param>
    /// <param name="messageQueue">Messaging service</param>
    public UpdateQuoteHandler(IEsRepository<IAggregate> repository, ICommandHandler<AddQuote> handler, ICommandHandler<RequestJson<T>> jsonRequestHandler, IMessageQueue messageQueue) 
    : base(repository)
    {
      _repository = repository;
      _handler = handler;
      _jsonRequestHandler = jsonRequestHandler;
      _messageQueue = messageQueue;
    }

    /// <inheritdoc/>
    public override async Task Handle(UpdateQuote<T> command)
    {
      var root = await _repository.Find<AssetPair>(command.Target);
      if (root == null)
        throw new ArgumentNullException(nameof(AssetPair));
      
      var dateFormat = Api.Fx.DateFormat;
      var url = Api.Fx.Url(root.ForAsset, root.DomAsset);
      if (typeof(T) == typeof(Api.Coin.JsonResult))
      {
        dateFormat = Api.Coin.DateFormat;
        url = Api.Coin.Url(root.ForAsset, root.DomAsset);
      }

      if (root.Url != null)
        url = root.Url;
      
      url = url.Replace("$date", command.Timestamp.ToString(dateFormat, new DateTimeFormatInfo())); 

      var obs = _messageQueue.Alerts.OfType<JsonRequestCompleted<T>>().Replay();
      obs.Connect();
      
      await _jsonRequestHandler.Handle(new RequestJson<T>(command.Target, url)).Timeout();

      var res = await obs.FirstOrDefaultAsync(r => r.RequestorId == command.Target).Timeout(Configuration.Timeout);
      ICommand addQuoteCommand;
      if (res.Data is Api.Fx.JsonResult fxResult)
        addQuoteCommand = new AddQuote(command.Target, command.Timestamp.ToInstant(), fxResult.Rates.USD);
      else if (res.Data is Api.Coin.JsonResult coinResult)
        addQuoteCommand = new AddQuote(command.Target, command.Timestamp.ToInstant(), coinResult.Market_Data.Current_price.Usd);
      else
        throw new InvalidCastException();

      addQuoteCommand.StoreInLog = false;
      await _handler.Handle(addQuoteCommand);
      command.EventType = addQuoteCommand.EventType;
    }

    /// <inheritdoc/>
    Task ICommandHandler<UpdateQuote>.Handle(UpdateQuote iCommand)
    {
      return Handle(iCommand as UpdateQuote<T>);
    }

    /// <inheritdoc/>
    protected override void Act(AssetPair root, UpdateQuote<T> command)
    {
      throw new System.NotImplementedException();
    }
  }
}
