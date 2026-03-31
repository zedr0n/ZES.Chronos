using System;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using NodaTime.Extensions;
using ZES.Infrastructure;
using ZES.Infrastructure.Alerts;
using ZES.Infrastructure.Net;
using ZES.Infrastructure.Utils;
using ZES.Interfaces.Clocks;
using ZES.Interfaces.Domain;
using ZES.Interfaces.EventStore;
using ZES.Interfaces.Infrastructure;

namespace Chronos.Core.Commands
{
  /// <inheritdoc />
  public class UpdateQuoteHandler : ZES.Infrastructure.Domain.CommandHandlerBase<UpdateQuote, AssetPair>
  {
    private readonly IEsRepository<IAggregate> _repository;
    private readonly IUpdateCommandFactory _factory;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateQuoteHandler"/> class.
    /// Responsible for handling the `UpdateQuote` command by updating the quotes of a specific asset pair
    /// and invoking necessary sub-commands and handlers through the command factory.
    /// </summary>
    /// <param name="repository">The aggregate repository used to retrieve and manage asset pair aggregates.</param>
    /// <param name="factory">Factory for creating sub-commands and their corresponding handlers.</param>
    /// <param name="clock">Clock used for time-related operations during quote updates.</param>
    /// <remarks>
    /// Ensures proper validation and processing of quote updates, including checks to prevent duplicate updates
    /// for the same date and determining whether the update is intraday or not.
    /// </remarks>
    public UpdateQuoteHandler(IEsRepository<IAggregate> repository, IUpdateCommandFactory factory, IClock clock)
      : base(repository)
    {
      _repository = repository;
      _factory = factory;
      _clock = clock;
    }

    /// <inheritdoc/>
    public override async Task Handle(UpdateQuote command)
    {
      var root = await _repository.Find<AssetPair>(command.Target);
      if (root == null)
        throw new ArgumentNullException(nameof(AssetPair));

      var now = _clock.GetCurrentInstant();
      var intraday = command.Timestamp.ToInstant().Minus(now.ToInstant()).Days >= 0;
      
      if (!intraday && root.QuoteDates.Any(d =>
            d.InUtc().Year == command.Timestamp.ToInstant().InUtc().Year &&
            d.InUtc().Month == command.Timestamp.ToInstant().InUtc().Month &&
            d.InUtc().Day == command.Timestamp.ToInstant().InUtc().Day))
      {
        throw new InvalidOperationException(
          $"Quote already added for {command.Timestamp.ToInstant().InUtc().ToString("yyyy-MM-dd", new DateTimeFormatInfo())}");
      }
      
      var (commandT, handler) = _factory.CreateUpdateQuote(command, root.ForAsset.AssetType, root.DomAsset.AssetType, intraday);
      await handler.Handle(commandT);
    }

    /// <inheritdoc/>
    protected override void Act(AssetPair root, UpdateQuote command)
    {
      throw new NotImplementedException();
    }
  }

  /// <inheritdoc cref="ZES.Interfaces.Domain.ICommandHandler" />
  public class UpdateQuoteHandler<T> : ZES.Infrastructure.Domain.CommandHandlerBase<UpdateQuote<T>, AssetPair>, ICommandHandler<UpdateQuote>
    where T : class, IJsonQuoteResult
  {
    private readonly ICommandHandler<AddQuote> _handler;
    private readonly ICommandHandler<RequestJson<T>> _jsonRequestHandler;
    private readonly ICommandHandler<RequestJson<Api.TickerSearch.JsonResult>> _tickerSearchHandler;
    private readonly IEsRepository<IAggregate> _repository;
    private readonly ICommandHandler<AddQuoteTicker> _addQuoteTickerHandler;
    private readonly IMessageQueue _messageQueue;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateQuoteHandler{T}"/> class.
    /// Handles the `UpdateQuote` command, orchestrating the process of updating quotes for asset pairs using
    /// various sub-commands such as adding quotes, processing JSON responses, and searching for tickers.
    /// </summary>
    /// <param name="repository">The aggregate repository for managing and retrieving domain aggregates.</param>
    /// <param name="addQuoteTickerHandler">The handler responsible for processing `AddQuoteTicker` commands.</param>
    /// <param name="handler">The handler responsible for processing `AddQuote` commands.</param>
    /// <param name="jsonRequestHandler">The handler responsible for requesting and processing JSON data of type <typeparamref name="T"/>.</param>
    /// <param name="tickerSearchHandler">The handler responsible for managing JSON requests related to ticker searches.</param>
    /// <param name="messageQueue">The messaging service for publishing and managing inter-process messages.</param>
    /// <remarks>
    /// This handler ensures that updates are executed efficiently by invoking necessary handlers in a coordinated manner and leverages
    /// repository and messaging services to maintain the state and communication. The command structure requires proper validation
    /// and processing logic to avoid duplicates and inaccuracies during quote updates.
    /// </remarks>
    public UpdateQuoteHandler(
      IEsRepository<IAggregate> repository,
      ICommandHandler<AddQuoteTicker> addQuoteTickerHandler,
      ICommandHandler<AddQuote> handler,
      ICommandHandler<RequestJson<T>> jsonRequestHandler,
      ICommandHandler<RequestJson<Api.TickerSearch.JsonResult>> tickerSearchHandler,
      IMessageQueue messageQueue)
      : base(repository)
    {
      _repository = repository;
      _addQuoteTickerHandler = addQuoteTickerHandler;
      _handler = handler;
      _jsonRequestHandler = jsonRequestHandler;
      _tickerSearchHandler = tickerSearchHandler;
      _messageQueue = messageQueue;
    }

    /// <inheritdoc/>
    public override async Task Handle(UpdateQuote<T> command)
    {
      var root = await _repository.Find<AssetPair>(command.Target);
      if (root == null)
        throw new ArgumentNullException(nameof(AssetPair));

      var ticker = root.Ticker;
      if (ticker == null)
      {
          var obsTicker = _messageQueue.Alerts.OfType<JsonRequestCompleted<Api.TickerSearch.JsonResult>>().Replay();
          obsTicker.Connect();

          var searchTicker = T.GetSearchTicker(command.Target, root.DomAsset, root.ForAsset);
      
          await _tickerSearchHandler.Handle(new RequestJson<Api.TickerSearch.JsonResult>(command.Target, Api.TickerSearch.JsonResult.GetUrl(searchTicker))).Timeout();
          var resTicker = await obsTicker.FirstOrDefaultAsync(r => r.RequestorId == command.Target).Timeout(Configuration.Timeout); 
          
          ticker = Api.TickerSearch.JsonResult.GetTicker(resTicker.Data);
          var addQuoteTickerCommand = new AddQuoteTicker(command.Target, ticker);
          await _addQuoteTickerHandler.Handle(addQuoteTickerCommand);
      }
      
      var dateFormat = T.GetDateFormat();

      var url = root.Url ?? T.GetUrl(ticker, command.EnforceCache);
      url = url.Replace("$date", command.Timestamp.ToString(dateFormat, new DateTimeFormatInfo())); 

      var obs = _messageQueue.Alerts.OfType<JsonRequestCompleted<T>>().Replay();
      obs.Connect();
      
      await _jsonRequestHandler.Handle(new RequestJson<T>(command.Target, url)).Timeout();

      var res = await obs.FirstOrDefaultAsync(r => r.RequestorId == command.Target).Timeout(Configuration.Timeout);
      var addQuoteCommand = new AddQuote(command.Target, command.Timestamp.ToInstant(), T.GetValue(res.Data, root.DomAsset))
        {
          StoreInLog = false,
        };

      await _handler.Handle(addQuoteCommand);
    }

    /// <inheritdoc/>
    Task ICommandHandler<UpdateQuote>.Handle(UpdateQuote iCommand) => Handle(iCommand as UpdateQuote<T>); 
    
    /// <inheritdoc/>
    protected override void Act(AssetPair root, UpdateQuote<T> command)
    {
      throw new System.NotImplementedException();
    }
  }
}
