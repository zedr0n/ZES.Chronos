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
    private readonly IUpdateQuoteCommandFactory _factory;
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
    public UpdateQuoteHandler(IEsRepository<IAggregate> repository, IUpdateQuoteCommandFactory factory, IClock clock)
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

      if (root.QuoteDates.Any(d =>
            d.InUtc().Year == command.Timestamp.ToInstant().InUtc().Year &&
            d.InUtc().Month == command.Timestamp.ToInstant().InUtc().Month &&
            d.InUtc().Day == command.Timestamp.ToInstant().InUtc().Day))
      {
        throw new InvalidOperationException(
          $"Quote already added for {command.Timestamp.ToInstant().InUtc().ToString("yyyy-MM-dd", new DateTimeFormatInfo())}");
      }
      
      var now = _clock.GetCurrentInstant();
      var intraday = command.Timestamp.ToInstant().Minus(now.ToInstant()).Days >= 0;

      var (commandT, handler) = _factory.Create(command.Target, root.ForAsset.AssetType, root.DomAsset.AssetType, intraday);
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
      
      var dateFormat = T.GetDateFormat();
      var url = T.GetUrl(root.ForAsset, root.DomAsset);

      if (root.Url != null)
        url = root.Url;
      
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
