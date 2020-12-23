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
  public class UpdateQuoteHandler : ZES.Infrastructure.Domain.CommandHandlerBase<UpdateQuote, AssetPair>
  {
    private readonly IEsRepository<IAggregate> _repository;
    private readonly ICollection<ICommandHandler<UpdateQuote>> _handlers;
    
    public UpdateQuoteHandler(IEsRepository<IAggregate> repository, ICollection<ICommandHandler<UpdateQuote>> handlers) 
      : base(repository)
    {
      _repository = repository;
      _handlers = handlers;
    }

    public override async Task Handle(UpdateQuote command)
    {
      var root = await _repository.Find<AssetPair>(command.Target);
      if (root == null)
        throw new ArgumentNullException(nameof(AssetPair));

      ICommandHandler handler = null;
      ICommand commandT = null;
      if (root.ForAsset.AssetType == Asset.Type.Currency && root.DomAsset.AssetType == Asset.Type.Currency)
      {
        commandT = new UpdateQuote<Api.Fx.JsonResult>(command.Target);
        handler = _handlers.SingleOrDefault(h => h.CanHandle(commandT));
      }
      else if (root.ForAsset.AssetType == Asset.Type.Coin && root.DomAsset.AssetType == Asset.Type.Currency)
      {
        commandT = new UpdateQuote<Api.Coin.JsonResult>(command.Target);
        handler = _handlers.SingleOrDefault(h => h.CanHandle(commandT));
      }
      
      if (handler == null)
        throw new InvalidOperationException($"Automatic quote retrieval for {root.ForAsset.Ticker}{root.DomAsset.Ticker} not supported");
      
      await handler.Handle(commandT);
      command.EventType = commandT.EventType;
    }

    protected override void Act(AssetPair root, UpdateQuote command)
    {
      throw new NotImplementedException();
    }
  }
  
  public class UpdateQuoteHandler<T> : ZES.Infrastructure.Domain.CommandHandlerBase<UpdateQuote<T>, AssetPair>, ICommandHandler<UpdateQuote>
    where T : class, IJsonResult
  {
    private readonly ICommandHandler<AddQuote> _handler;
    private readonly ICommandHandler<RequestJson<T>> _jsonRequestHandler;
    private readonly IEsRepository<IAggregate> _repository;
    private readonly IMessageQueue _messageQueue;

    public UpdateQuoteHandler(IEsRepository<IAggregate> repository, ICommandHandler<AddQuote> handler, ICommandHandler<RequestJson<T>> jsonRequestHandler, IMessageQueue messageQueue) 
      : base(repository)
    {
      _repository = repository;
      _handler = handler;
      _jsonRequestHandler = jsonRequestHandler;
      _messageQueue = messageQueue;
    }

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
      await _jsonRequestHandler.Handle(new RequestJson<T>(command.Target, url)).Timeout();
      
      var res = await _messageQueue.Alerts.OfType<JsonRequestCompleted<T>>().FirstOrDefaultAsync(r => r.RequestorId == command.Target).Timeout(Configuration.Timeout);
      ICommand addQuoteCommand;
      if (res.Data is Api.Fx.JsonResult fxResult)
        addQuoteCommand = new AddQuote(command.Target, command.Timestamp, fxResult.Rates.USD);
      else if (res.Data is Api.Coin.JsonResult coinResult)
        addQuoteCommand = new AddQuote(command.Target, command.Timestamp, coinResult.Market_Data.Current_price.Usd);
      else
        throw new InvalidCastException();

      await _handler.Handle(addQuoteCommand);
      command.EventType = addQuoteCommand.EventType;
    }

    Task ICommandHandler<UpdateQuote>.Handle(UpdateQuote iCommand)
    {
      return Handle(iCommand as UpdateQuote<T>);
    }
    
    protected override void Act(AssetPair root, UpdateQuote<T> command)
    {
      throw new System.NotImplementedException();
    }
  }
}
