using System;
using System.Collections.Generic;
using System.Linq;
using Chronos.Core.Commands;
using NodaTime.Extensions;
using ZES.Infrastructure.Utils;
using ZES.Interfaces.Domain;

namespace Chronos.Core;

/// <inheritdoc />
public class UpdateQuoteCommandFactory(ICollection<ICommandHandler<UpdateQuote>> handlers) : IUpdateQuoteCommandFactory
{
    /// <inheritdoc/>
    public (ICommand Command, ICommandHandler Handler) Create(string target, AssetType forAssetType, AssetType domAssetType, bool intraday)
    {
        return (forAssetType, domAssetType) switch
        {
            (AssetType.Currency, AssetType.Currency) => intraday ? CreateTyped<Api.Fx.LiveJsonResult>(target) : CreateTyped<Api.Fx.JsonResult>(target),
            (AssetType.Coin, AssetType.Currency) => intraday ? CreateTyped<Api.Coin.LiveJsonResult>(target) : CreateTyped<Api.Coin.JsonResult>(target),
            (AssetType.Equity, AssetType.Currency) => intraday ? CreateTyped<Api.Equity.LiveJsonResult>(target) : CreateTyped<Api.Equity.JsonResult>(target),
            _ => throw new InvalidOperationException($"Automatic quote retrieval for {forAssetType}/{domAssetType} not supported"),
        };
    }

    private (ICommand Command, ICommandHandler Handler) CreateTyped<T>(string target) 
        where T : class, IJsonQuoteResult
    {
        var command = new UpdateQuote<T>(target);
        var handler = handlers.SingleOrDefault(h => h.CanHandle(command));
        return (command, handler);
    }
}