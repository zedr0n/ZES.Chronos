using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Chronos.Core.Net;
using ZES.Infrastructure;
using ZES.Infrastructure.Alerts;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.Net;
using ZES.Infrastructure.Utils;
using ZES.Interfaces;
using ZES.Interfaces.Domain;
using ZES.Interfaces.EventStore;
using ZES.Interfaces.Infrastructure;
using ZES.Interfaces.Net;

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
/// Handles the <see cref="UpdateTicker{T, TSearch}"/> command to update ticker information for the specified <see cref="AssetPair"/>.
/// </summary>
/// <typeparam name="T">The type of quote result implementing <see cref="IJsonResult"/>.</typeparam>
/// <typeparam name="TSearch">The type of quote search result implementing <see cref="IJsonResult"/></typeparam>
public class UpdateTickerHandler<T, TSearch>(
    IEsRepository<IAggregate> repository,
    IWebApiProvider webApiProvider,
    ICommandHandler<RequestJson<TSearch>> tickerSearchHandler,
    ICommandHandler<AddQuoteTicker> addQuoteTickerHandler,
    IMessageQueue messageQueue)
    : CommandHandlerBase<UpdateTicker<T, TSearch>, AssetPair>(repository), ICommandHandler<UpdateTicker>
    where T : class, IWebQuoteJsonResult
    where TSearch : class, IWebSearchJsonResult
{
    private readonly IEsRepository<IAggregate> _repository = repository;

    /// <inheritdoc/>
    public override async Task Handle(UpdateTicker<T, TSearch> command)
    {
        var root = await _repository.Find<AssetPair>(command.Target);
        if (root == null)
            throw new ArgumentNullException(nameof(AssetPair));

        if (root.Ticker != null)
            return;

        var ticker = string.Empty;
        switch (root.ForAsset.AssetId)
        {
            case "GBX" when root.DomAsset.AssetId == "GBP":
            case "GBP" when root.DomAsset.AssetId == "GBX":
                break;
            default:
            {
                var webSearchApi = webApiProvider.GetSearchApi();
                var webQuoteApi = webApiProvider.GetQuoteApi(root.ForAsset.AssetType, root.DomAsset.AssetType, false);
        
                var obsTicker = messageQueue.Alerts.OfType<JsonRequestCompleted<TSearch>>().Replay();
                obsTicker.Connect();

                var searchTicker = webQuoteApi.GetSearchTicker(root.ForAsset, root.DomAsset);
     
                await tickerSearchHandler.Handle(new RequestJson<TSearch>(command.Target, webSearchApi.GetUrl(searchTicker))).Timeout();
                var resTicker = await obsTicker.FirstOrDefaultAsync(r => r.RequestorId == command.Target).Timeout(Configuration.Timeout); 
          
                ticker = webSearchApi.GetTicker(resTicker.Data);
                var ccy = webSearchApi.GetCurrency(resTicker.Data);
                if (root.DomAsset.AssetId != ccy)
                    throw new InvalidOperationException($"Currency of the ticker is not matching the domestic asset: {ccy} != {root.DomAsset.AssetId}");
                break;
            }
        }
        
        var addQuoteTickerCommand = new AddQuoteTicker(command.Target, ticker) { CorrelationId = command.CorrelationId };
        await addQuoteTickerHandler.Handle(addQuoteTickerCommand);
    }

    /// <inheritdoc/>
    Task ICommandHandler<UpdateTicker>.Handle(UpdateTicker iCommand) => Handle(iCommand as UpdateTicker<T, TSearch>); 
    
    /// <inheritdoc/>
    protected override void Act(AssetPair root, UpdateTicker<T, TSearch> command)
    {
        throw new System.NotImplementedException();
    }
}