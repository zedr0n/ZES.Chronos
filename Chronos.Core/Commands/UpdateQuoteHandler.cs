using System;
using System.Globalization;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ZES.Infrastructure;
using ZES.Infrastructure.Alerts;
using ZES.Infrastructure.Net;
using ZES.Infrastructure.Utils;
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

    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateQuoteHandler"/> class.
    /// Handles the `UpdateQuote` command, updating quotes for a specific asset pair
    /// and managing the necessary business logic for processing updates, including
    /// invoking command factories to generate sub-commands and handlers.
    /// </summary>
    /// <param name="repository">Aggregate repository</param>
    /// <param name="factory">Command factory</param>
    /// <remarks>
    /// This handler is responsible for ensuring that quotes are updated for a given
    /// target asset pair and validating that updates align with the intended rules
    /// (e.g., no duplicate updates for the same date).
    /// </remarks>
    public UpdateQuoteHandler(IEsRepository<IAggregate> repository, IUpdateQuoteCommandFactory factory)
    : base(repository)
    {
      _repository = repository;
      _factory = factory;
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

      var (commandT, handler) = _factory.Create(command.Target, root.ForAsset.AssetType, root.DomAsset.AssetType);
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
