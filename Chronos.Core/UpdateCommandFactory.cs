using System;
using System.Collections.Generic;
using System.Linq;
using Chronos.Core.Commands;
using ZES.Interfaces.Domain;

namespace Chronos.Core;

/// <inheritdoc />
public class UpdateCommandFactory(ICollection<ICommandHandler<UpdateQuote>> updateQuoteHandlers, ICollection<ICommandHandler<UpdateTicker>> updateTickerHandlers) : IUpdateCommandFactory
{
    /// <inheritdoc/>
    public (ICommand Command, ICommandHandler Handler) CreateUpdateQuote(UpdateQuote command, AssetType forAssetType, AssetType domAssetType, bool intraday)
    {
        return (forAssetType, domAssetType) switch
        {
            (AssetType.Currency, AssetType.Currency) => intraday ? CreateTypedUpdateQuote<Api.Fx.LiveJsonResult>(command) : CreateTypedUpdateQuote<Api.Fx.JsonResult>(command),
            (AssetType.Coin, AssetType.Currency) => intraday ? CreateTypedUpdateQuote<Api.Coin.LiveJsonResult>(command) : CreateTypedUpdateQuote<Api.Coin.JsonResult>(command),
            (AssetType.Equity, AssetType.Currency) => intraday ? CreateTypedUpdateQuote<Api.Equity.LiveJsonResult>(command) : CreateTypedUpdateQuote<Api.Equity.JsonResult>(command),
            _ => throw new InvalidOperationException($"Automatic quote retrieval for {forAssetType}/{domAssetType} not supported"),
        };
    }

    /// <inheritdoc/>
    public (ICommand Command, ICommandHandler Handler) CreateUpdateTicker(UpdateTicker command, AssetType forAssetType, AssetType domAssetType)
    {
        return (forAssetType, domAssetType) switch
        {
            (AssetType.Currency, AssetType.Currency) => CreateTypedUpdateTicker<Api.Fx.JsonResult>(command),
            (AssetType.Coin, AssetType.Currency) => CreateTypedUpdateTicker<Api.Coin.JsonResult>(command), 
            (AssetType.Equity, AssetType.Currency) => CreateTypedUpdateTicker<Api.Equity.JsonResult>(command), 
            _ => throw new InvalidOperationException($"Automatic quote retrieval for {forAssetType}/{domAssetType} not supported"),
        };
    }

    private (ICommand Command, ICommandHandler Handler) CreateTypedUpdateTicker<T>(UpdateTicker command) 
        where T : class, IJsonQuoteResult
    {
        var commandT = new UpdateTicker<T>(command);
        var handler = updateTickerHandlers.SingleOrDefault(h => h.CanHandle(commandT));
        return (commandT, handler);
    }
    
    private (ICommand Command, ICommandHandler Handler) CreateTypedUpdateQuote<T>(UpdateQuote command) 
        where T : class, IJsonQuoteResult
    {
        var commandT = new UpdateQuote<T>(command);
        var handler = updateQuoteHandlers.SingleOrDefault(h => h.CanHandle(commandT));
        return (commandT, handler);
    }
}