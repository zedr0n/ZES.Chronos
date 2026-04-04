using System;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Chronos.Core.Net;
using NodaTime.Extensions;
using ZES.Infrastructure;
using ZES.Infrastructure.Alerts;
using ZES.Infrastructure.Net;
using ZES.Infrastructure.Utils;
using ZES.Interfaces.Clocks;
using ZES.Interfaces.Domain;
using ZES.Interfaces.EventStore;
using ZES.Interfaces.Infrastructure;
using ZES.Interfaces.Net;

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
    where T : class, IJsonResult
  {
    private readonly ICommandHandler<AddQuote> _handler;
    private readonly IWebApiProvider _webApiProvider;
    private readonly ICommandHandler<RequestJson<T>> _jsonRequestHandler;
    private readonly ICommandHandler<RequestJson<WebSearchApi.JsonResult>> _tickerSearchHandler;
    private readonly IEsRepository<IAggregate> _repository;
    private readonly ICommandHandler<AddQuoteTicker> _addQuoteTickerHandler;
    private readonly IMessageQueue _messageQueue;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateQuoteHandler{T}"/> class.
    /// Facilitates handling the `UpdateQuote` command by coordinating the update process
    /// for quotes of asset pairs, leveraging multiple sub-command handlers and services.
    /// </summary>
    /// <param name="repository">The repository used for managing and retrieving domain aggregates in the event store.</param>
    /// <param name="addQuoteTickerHandler">Handler responsible for managing `AddQuoteTicker` commands.</param>
    /// <param name="handler">Handler responsible for managing `AddQuote` commands to apply quote updates.</param>
    /// <param name="webApiProvider">Service provider for interfacing with external Web API endpoints.</param>
    /// <param name="jsonRequestHandler">Handler for processing JSON data retrieval for objects of type <typeparamref name="T"/>.</param>
    /// <param name="tickerSearchHandler">Handler for managing JSON requests specific to ticker searches.</param>
    /// <param name="messageQueue">Service for communicating across processes using a message queuing system.</param>
    /// <remarks>
    /// This handler is designed to ensure robust and efficient handling of quote updates by coordinating
    /// relevant sub-command handlers and external Web APIs. It enforces validation, processes data integrity
    /// checks, and coordinates messaging to avoid inconsistencies during updates.
    /// </remarks>
    public UpdateQuoteHandler(
      IEsRepository<IAggregate> repository,
      ICommandHandler<AddQuoteTicker> addQuoteTickerHandler,
      ICommandHandler<AddQuote> handler,
      IWebApiProvider webApiProvider,
      ICommandHandler<RequestJson<T>> jsonRequestHandler,
      ICommandHandler<RequestJson<WebSearchApi.JsonResult>> tickerSearchHandler,
      IMessageQueue messageQueue)
      : base(repository)
    {
      _repository = repository;
      _addQuoteTickerHandler = addQuoteTickerHandler;
      _handler = handler;
      _webApiProvider = webApiProvider;
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

      var webQuoteApi = _webApiProvider.GetQuoteApi(root.ForAsset.AssetType, root.DomAsset.AssetType, command.EnforceCache);
      var webSearchApi = _webApiProvider.GetSearchApi();
      var ticker = root.Ticker;
      if (ticker == null)
      {
        var obsTicker = _messageQueue.Alerts.OfType<JsonRequestCompleted<WebSearchApi.JsonResult>>().Replay();
        obsTicker.Connect();

        var searchTicker = webQuoteApi.GetSearchTicker(root.ForAsset, root.DomAsset);

        await _tickerSearchHandler
          .Handle(new RequestJson<WebSearchApi.JsonResult>(command.Target, webSearchApi.GetUrl(searchTicker))).Timeout();
        var resTicker = await obsTicker.FirstOrDefaultAsync(r => r.RequestorId == command.Target).Timeout(Configuration.Timeout); 
          
        ticker = webSearchApi.GetTicker(resTicker.Data);
        var addQuoteTickerCommand = new AddQuoteTicker(command.Target, ticker);
        await _addQuoteTickerHandler.Handle(addQuoteTickerCommand);
      }
      
      var url = root.Url ?? webQuoteApi.GetUrl(ticker, command.Timestamp, command.EnforceCache);

      var obs = _messageQueue.Alerts.OfType<JsonRequestCompleted<T>>().Replay();
      obs.Connect();
      
      await _jsonRequestHandler.Handle(new RequestJson<T>(command.Target, url)).Timeout();

      var res = await obs.FirstOrDefaultAsync(r => r.RequestorId == command.Target).Timeout(Configuration.Timeout);
      var addQuoteCommand = new AddQuote(command.Target, command.Timestamp.ToInstant(), webQuoteApi.GetValue(res.Data))
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
