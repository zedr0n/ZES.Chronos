using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Chronos.Core.Net;
using ZES.Infrastructure;
using ZES.Infrastructure.Alerts;
using ZES.Infrastructure.Net;
using ZES.Infrastructure.Utils;
using ZES.Interfaces.Clocks;
using ZES.Interfaces.Domain;
using ZES.Interfaces.EventStore;
using ZES.Interfaces.Infrastructure;
using IClock = ZES.Interfaces.Clocks.IClock;

namespace Chronos.Core.Commands
{
  /// <inheritdoc />
  public class UpdateQuoteHandler : ZES.Infrastructure.Domain.CommandHandlerBase<UpdateQuote, AssetPair>
  {
    private readonly IEsRepository<IAggregate> _repository;
    private readonly IUpdateCommandFactory _factory;
    private readonly IClock _clock;
    private readonly IErrorLog _errorLog;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateQuoteHandler"/> class.
    /// Responsible for handling the `UpdateQuote` command by updating the quotes of a specific asset pair
    /// and invoking necessary sub-commands and handlers through the command factory.
    /// </summary>
    /// <param name="repository">The aggregate repository used to retrieve and manage asset pair aggregates.</param>
    /// <param name="factory">Factory for creating sub-commands and their corresponding handlers.</param>
    /// <param name="clock">Clock used for time-related operations during quote updates.</param>
    /// <param name="errorLog">Error log used to track and manage errors during quote updates.</param>
    /// <remarks>
    /// Ensures proper validation and processing of quote updates, including checks to prevent duplicate updates
    /// for the same date and determining whether the update is intraday or not.
    /// </remarks>
    public UpdateQuoteHandler(IEsRepository<IAggregate> repository, IUpdateCommandFactory factory, IClock clock, IErrorLog errorLog)
      : base(repository)
    {
      _repository = repository;
      _factory = factory;
      _clock = clock;
      _errorLog = errorLog;
    }

    /// <inheritdoc/>
    public override async Task Handle(UpdateQuote command)
    {
      var root = await _repository.Find<AssetPair>(command.Target);
      if (root == null)
        throw new ArgumentNullException(nameof(AssetPair));

      var holidayCalendar = root.HolidayCalendar;
      
      var timestamp = command.Timestamp;
      var now = _clock.GetCurrentInstant();
      var intraday = timestamp.ToDateTime().Date == now.ToDateTime().Date;
      
      // handle intraday
      if (intraday && !root.SupportsIntraday)
      {
        intraday = false;
        timestamp = timestamp.PreviousWorkingDay().CloseOfDay(); 
      }

      // handle holidays
      if (holidayCalendar != null && !timestamp.IsWorkingDay())
      {
        timestamp = timestamp.PreviousWorkingDay().CloseOfDay();
        intraday = false;
      }
      
      var additionalTimestamps = command.AdditionalTimestamps;
      if (command.AdditionalTimestamps != null && holidayCalendar != null)
      {
        additionalTimestamps = command.AdditionalTimestamps
          .Select(t => !t.IsWorkingDay() ? t.PreviousWorkingDay().CloseOfDay() : t).Distinct().Where(t => t != timestamp).ToList();
      }

      if (!command.Ephemeral && additionalTimestamps is { Count: > 0 })
        throw new InvalidOperationException("Additional timestamps can only be specified for ephemeral quote updates");
      
      var hasExplicitTime = timestamp != timestamp.StartOfDay();
      
      if (timestamp == null)
        throw new InvalidOperationException("Timestamp cannot be null");

      bool withinSameDay;
      if (holidayCalendar != null)
        withinSameDay = root.QuoteDates.Any(d => d.IsWithinPriorWorkingDays(timestamp.ToInstant(), timestamp.IsWorkingDay() ? 0 : 1));  
      else
        withinSameDay = root.QuoteDates.Any(d => d.IsSameDay(timestamp.ToInstant()));
      
      if (!intraday && !hasExplicitTime && withinSameDay) 
      {
        throw new InvalidOperationException(
          $"Quote already added for {timestamp.ToInstant().InUtc().ToString("yyyy-MM-dd", new DateTimeFormatInfo())}");
      }

      if (intraday && additionalTimestamps is { Count: > 0 })
      {
        var (intradayCommandT, intradayHandler) = _factory.CreateUpdateQuote(command, root.ForAsset.AssetType, root.DomAsset.AssetType, timestamp, null, true);
        await intradayHandler.Handle(intradayCommandT);

        timestamp = additionalTimestamps.Max();
        additionalTimestamps = additionalTimestamps.Where(t => t != timestamp).OrderBy(t => t).ToList();
        intraday = false;
      }
      
      var (commandT, handler) = _factory.CreateUpdateQuote(command, root.ForAsset.AssetType, root.DomAsset.AssetType, timestamp, additionalTimestamps, intraday);
      await handler.Handle(commandT);
    }

    /// <inheritdoc/>
    protected override void Act(AssetPair root, UpdateQuote command)
    {
      throw new NotImplementedException();
    }
  }

  /// <inheritdoc cref="ZES.Interfaces.Domain.ICommandHandler" />
  public class UpdateQuoteHandler<T, TSearch> : ZES.Infrastructure.Domain.CommandHandlerBase<UpdateQuote<T, TSearch>, AssetPair>, ICommandHandler<UpdateQuote>
    where T : class, IWebQuoteJsonResult
    where TSearch : class, IWebSearchJsonResult
  {
    private readonly ICommandHandler<AddQuote> _handler;
    private readonly IWebApiProvider _webApiProvider;
    private readonly ICommandHandler<RequestJson<T>> _jsonRequestHandler;
    private readonly ICommandHandler<RequestJson<TSearch>> _tickerSearchHandler;
    private readonly IEsRepository<IAggregate> _repository;
    private readonly ICommandHandler<AddQuoteTicker> _addQuoteTickerHandler;
    private readonly IMessageQueue _messageQueue;
    private readonly IClock _clock;

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateQuoteHandler{T, TSearch}"/> class.
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
    /// <param name="clock">Clock used for time-related operations during quote updates.</param>
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
      ICommandHandler<RequestJson<TSearch>> tickerSearchHandler,
      IMessageQueue messageQueue,
      IClock clock)
      : base(repository)
    {
      _repository = repository;
      _addQuoteTickerHandler = addQuoteTickerHandler;
      _handler = handler;
      _webApiProvider = webApiProvider;
      _jsonRequestHandler = jsonRequestHandler;
      _tickerSearchHandler = tickerSearchHandler;
      _messageQueue = messageQueue;
      _clock = clock;
    }

    /// <inheritdoc/>
    public override async Task Handle(UpdateQuote<T, TSearch> command)
    {
      var root = await _repository.Find<AssetPair>(command.Target);
      if (root == null)
        throw new ArgumentNullException(nameof(AssetPair));

      var now = _clock.GetCurrentInstant();
      var intraday = command.Timestamp.ToDateTime().Date == now.ToDateTime().Date;
      
      var webQuoteApi = _webApiProvider.GetQuoteApi(root.ForAsset.AssetType, root.DomAsset.AssetType, intraday);
      var webSearchApi = _webApiProvider.GetSearchApi();
      var ticker = root.Ticker;
      if (ticker == null)
      {
        var obsTicker = _messageQueue.Alerts.OfType<JsonRequestCompleted<TSearch>>().Replay();
        obsTicker.Connect();

        var searchTicker = webQuoteApi.GetSearchTicker(root.ForAsset, root.DomAsset);

        await _tickerSearchHandler
          .Handle(new RequestJson<TSearch>(command.Target, webSearchApi.GetUrl(searchTicker))).Timeout();
        var resTicker = await obsTicker.FirstOrDefaultAsync(r => r.RequestorId == command.Target).Timeout(Configuration.Timeout); 
          
        ticker = webSearchApi.GetTicker(resTicker.Data);
        if (string.IsNullOrEmpty(ticker))
          return;
        
        var addQuoteTickerCommand = new AddQuoteTicker(command.Target, ticker);
        await _addQuoteTickerHandler.Handle(addQuoteTickerCommand);
      }

      var values = await ResolveValues(command, root, ticker, root.Url, webQuoteApi);
      foreach (var (timestamp, value) in values)
        await AddQuote(command, timestamp, value);
    }

    /// <inheritdoc/>
    Task ICommandHandler<UpdateQuote>.Handle(UpdateQuote iCommand) => Handle(iCommand as UpdateQuote<T, TSearch>); 
    
    /// <inheritdoc/>
    protected override void Act(AssetPair root, UpdateQuote<T, TSearch> command)
    {
      throw new System.NotImplementedException();
    }

    private async Task<IEnumerable<(Time Timestamp, double Value)>> ResolveValues(
      UpdateQuote<T, TSearch> command,
      AssetPair assetPair,
      string ticker,
      string urlOverride,
      IWebQuoteApi webQuoteApi)
    {
      var timestamps = new List<Time> { command.Timestamp };
      if (command.AdditionalTimestamps != null)
        timestamps.AddRange(command.AdditionalTimestamps);

      List<(Time Timestamp, double Value)> values = assetPair.DomAsset.AssetId switch
      {
        "GBX" when assetPair.ForAsset.AssetId == "GBP" => timestamps.Select(t => (t, 100.0)).ToList(),
        "GBP" when assetPair.ForAsset.AssetId == "GBX" => timestamps.Select(t => (t, 0.01)).ToList(),
        _ => (await QueryValues(command, timestamps, ticker, urlOverride, webQuoteApi)).ToList(),
      };

      return values;
    }

    private async Task AddQuote(UpdateQuote<T, TSearch> command, Time timestamp, double value)
    {
      var addQuoteCommand = new AddQuote(command.Target, timestamp.ToInstant(), value)
      {
        StoreInLog = false,
        CorrelationId = command.CorrelationId,
        AncestorId = command.AncestorId ?? command.MessageId,
      };
      
      if (command.Ephemeral)
      {
        addQuoteCommand.Ephemeral = true;
        addQuoteCommand.Timestamp = timestamp;
      }

      await _handler.Handle(addQuoteCommand);
    }

    private async Task<IEnumerable<(Time Timestamp, double Value)>> QueryValues(
      UpdateQuote<T, TSearch> command,
      IEnumerable<Time> timestamps,
      string ticker,
      string urlOverride,
      IWebQuoteApi webQuoteApi)
    {
      var dates = timestamps.ToList(); 
      
      if (!webQuoteApi.SupportsMultipleTimestamps() || dates.Count == 1 || !command.UseBatching)
      {
        var valuesList = new List<(Time Timestamp, double Value)>();
        foreach (var t in dates.OrderBy(t => t).ToList())
        {
          var value = await QueryValue(command, t, ticker, urlOverride, webQuoteApi);
          valuesList.Add((t, value));
        }

        return valuesList;
      }
      
      var obs = _messageQueue.Alerts.OfType<JsonRequestCompleted<T>>().Replay();
      obs.Connect();

      var timestamp = command.Timestamp;
      var noTimeSpecified = timestamp == timestamp.StartOfDay();
      
      var preciseUrl = urlOverride ?? webQuoteApi.GetPreciseUrl(ticker, dates, command.EnforceCache);
      var url = urlOverride ?? webQuoteApi.GetUrl(ticker, dates, command.EnforceCache);

      var values = dates.ToDictionary(x => x, x => double.NaN); 
      
      if (preciseUrl != null && preciseUrl != url && !noTimeSpecified)
      {
        var preciseRequestorId = $"{command.Target}-precise-{timestamp.ToUnixTimeSeconds()}";
        await _jsonRequestHandler.Handle(new RequestJson<T>(preciseRequestorId, preciseUrl)).Timeout();

        var resPrecise = await obs.FirstOrDefaultAsync(r => r.RequestorId == preciseRequestorId).Timeout(Configuration.Timeout);
        var preciseResults = webQuoteApi.GetValues(resPrecise.Data).ToList();
        var valid = true;
        foreach (var t in dates)
        {
          var v = preciseResults.SingleOrDefault(x => x.Timestamp == t);
          if ( v == default )
            v = preciseResults.SingleOrDefault(x => Math.Abs(x.Timestamp.ToUnixTimeSeconds() - t.ToUnixTimeSeconds()) < 60);
          if (v == default)
            v = (t, double.NaN);
          
          values[t] = v.Value;
          valid &= !double.IsNaN(v.Value);
        }
        
        if (valid)
          return values.Select(x => (x.Key, x.Value)).ToList();
      }
          
      var requestorId = $"{command.Target}-{timestamp.ToUnixTimeSeconds()}";
      await _jsonRequestHandler.Handle(new RequestJson<T>(requestorId, url)).Timeout();

      var res = await obs.FirstOrDefaultAsync(r => r.RequestorId == requestorId).Timeout(Configuration.Timeout);
      var results = webQuoteApi.GetValues(res.Data).ToList();

      foreach (var t in dates)
      {
        if (!double.IsNaN(values[t]))
          continue;
        var v = results.SingleOrDefault(x => x.Timestamp == t);
        if (v == default)
          v = results.SingleOrDefault(x => x.Timestamp.IsSameDay(t));
        if (v == default)
          v = (t, double.NaN);
        values[t] = v.Value;
      }
      
      return values.Select(x => (x.Key, x.Value)).ToList();
    }
    
    private async Task<double> QueryValue(
      UpdateQuote<T, TSearch> command, 
      Time timestamp,
      string ticker,
      string urlOverride,
      IWebQuoteApi webQuoteApi)
    {
      var obs = _messageQueue.Alerts.OfType<JsonRequestCompleted<T>>().Replay();
      obs.Connect();

      var noTimeSpecified = timestamp == timestamp.StartOfDay();
      
      var preciseUrl = urlOverride ?? webQuoteApi.GetPreciseUrl(ticker, timestamp, command.EnforceCache);
      var url = urlOverride ?? webQuoteApi.GetUrl(ticker, timestamp, command.EnforceCache);

      var value = 0.0;
      
      if (preciseUrl != null && preciseUrl != url && !noTimeSpecified)
      {
        var preciseRequestorId = $"{command.Target}-precise-{timestamp.ToUnixTimeSeconds()}";
        await _jsonRequestHandler.Handle(new RequestJson<T>(preciseRequestorId, preciseUrl)).Timeout();

        var resPrecise = await obs.FirstOrDefaultAsync(r => r.RequestorId == preciseRequestorId).Timeout(Configuration.Timeout);
        value = webQuoteApi.GetValue(resPrecise.Data).Value;
        if (!double.IsNaN(value))
          return value;
      }
          
      var requestorId = $"{command.Target}-{timestamp.ToUnixTimeSeconds()}";
      await _jsonRequestHandler.Handle(new RequestJson<T>(requestorId, url)).Timeout();

      var res = await obs.FirstOrDefaultAsync(r => r.RequestorId == requestorId).Timeout(Configuration.Timeout);
      value = webQuoteApi.GetValue(res.Data).Value;

      return value;
    }
  }
}
