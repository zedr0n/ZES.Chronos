using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using ZES.Infrastructure;
using ZES.Infrastructure.Alerts;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.Net;
using ZES.Infrastructure.Utils;
using ZES.Interfaces;
using ZES.Interfaces.Domain;
using ZES.Interfaces.EventStore;
using ZES.Interfaces.Infrastructure;

namespace Chronos.Core.Commands;

/// <summary>
/// Handles the <see cref="UpdateTicker"/> command to update the ticker for a specific <see cref="AssetPair"/>.
/// </summary>
public class UpdateTickerHandler(IEsRepository<IAggregate> repository, IUpdateCommandFactory commandFactory) 
    : CommandHandlerBase<UpdateTicker, AssetPair>(repository)
{
    private readonly IEsRepository<IAggregate> _repository = repository;

    /// <inheritdoc/>
    public override async Task Handle(UpdateTicker command)
    {
        var root = await _repository.Find<AssetPair>(command.Target);
        if (root == null)
            throw new ArgumentNullException(nameof(AssetPair));
        
        var (commandT, handler) = commandFactory.CreateUpdateTicker(command, root.ForAsset.AssetType, root.DomAsset.AssetType);
        await handler.Handle(commandT);
    }

    /// <inheritdoc/>
    protected override void Act(AssetPair root, UpdateTicker command)
    {
        throw new NotImplementedException();
    }
}

/// <summary>
/// Handles the <see cref="UpdateTicker{T}"/> command to update the ticker for an <see cref="AssetPair"/>.
/// </summary>
/// <typeparam name="T">The type of quote result that implements <see cref="IJsonQuoteResult"/>.</typeparam>
public class UpdateTickerHandler<T>(
    IEsRepository<IAggregate> repository,
    ICommandHandler<RequestJson<Api.TickerSearch.JsonResult>> tickerSearchHandler,
    ICommandHandler<AddQuoteTicker> addQuoteTickerHandler,
    IMessageQueue messageQueue)
    : CommandHandlerBase<UpdateTicker<T>, AssetPair>(repository), ICommandHandler<UpdateTicker>
    where T : class, IJsonQuoteResult
{
    private readonly IEsRepository<IAggregate> _repository = repository;

    /// <inheritdoc/>
    public override async Task Handle(UpdateTicker<T> command)
    {
        var root = await _repository.Find<AssetPair>(command.Target);
        if (root == null)
            throw new ArgumentNullException(nameof(AssetPair));

        if (root.Ticker != null)
            return;
        
        var obsTicker = messageQueue.Alerts.OfType<JsonRequestCompleted<Api.TickerSearch.JsonResult>>().Replay();
        obsTicker.Connect();

        var searchTicker = T.GetSearchTicker(command.Target, root.DomAsset, root.ForAsset);
      
        await tickerSearchHandler.Handle(new RequestJson<Api.TickerSearch.JsonResult>(command.Target, Api.TickerSearch.JsonResult.GetUrl(searchTicker))).Timeout();
        var resTicker = await obsTicker.FirstOrDefaultAsync(r => r.RequestorId == command.Target).Timeout(Configuration.Timeout); 
          
        var ticker = Api.TickerSearch.JsonResult.GetTicker(resTicker.Data);
        var addQuoteTickerCommand = new AddQuoteTicker(command.Target, ticker) { CorrelationId = command.CorrelationId };
        await addQuoteTickerHandler.Handle(addQuoteTickerCommand);
    }

    /// <inheritdoc/>
    Task ICommandHandler<UpdateTicker>.Handle(UpdateTicker iCommand) => Handle(iCommand as UpdateTicker<T>); 
    
    /// <inheritdoc/>
    protected override void Act(AssetPair root, UpdateTicker<T> command)
    {
        throw new System.NotImplementedException();
    }
}