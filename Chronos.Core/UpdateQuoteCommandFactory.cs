using System;
using System.Collections.Generic;
using System.Linq;
using Chronos.Core.Commands;
using ZES.Interfaces.Domain;

namespace Chronos.Core;

/// <inheritdoc />
public class UpdateQuoteCommandFactory(ICollection<ICommandHandler<UpdateQuote>> handlers) : IUpdateQuoteCommandFactory
{
    /// <inheritdoc/>
    public (ICommand Command, ICommandHandler Handler) Create(string target, AssetType forAssetType, AssetType domAssetType)
    {
        return (forAssetType, domAssetType) switch
        {
            (AssetType.Currency, AssetType.Currency) => CreateTyped<Api.Fx.JsonResult>(target),
            (AssetType.Coin, AssetType.Currency) => CreateTyped<Api.Coin.JsonResult>(target),
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