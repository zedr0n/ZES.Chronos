using System;
using System.Collections.Generic;
using System.Linq;
using Chronos.Core.Commands;
using Chronos.Core.Net;
using ZES.Interfaces.Domain;

namespace Chronos.Core;

/// <inheritdoc />
public class UpdateCommandFactory(ICollection<ICommandHandler<UpdateQuote>> updateQuoteHandlers, ICollection<ICommandHandler<UpdateTicker>> updateTickerHandlers, IWebApiProvider webApiProvider) : IUpdateCommandFactory
{
    /// <inheritdoc/>
    public (ICommand Command, ICommandHandler Handler) CreateUpdateQuote(UpdateQuote command, AssetType forAssetType, AssetType domAssetType, bool intraday)
    {
        var webApi = webApiProvider.GetQuoteApi(forAssetType, domAssetType, intraday);
        if (webApi == null)
           throw new InvalidOperationException($"No quote API found for {forAssetType}/{domAssetType}");
        
        var webSearchApi = webApiProvider.GetSearchApi();

        var t = webApi.GetJsonResultType();
        var tSearch = webSearchApi.GetJsonResultType();
        var commandT = (ICommand)Activator.CreateInstance(typeof(UpdateQuote<,>).MakeGenericType(t, tSearch), command);
        commandT?.StoreInLog = false;
        var handler = updateQuoteHandlers.SingleOrDefault(h => h.CanHandle(commandT));
        return (commandT, handler);
    }

    /// <inheritdoc/>
    public (ICommand Command, ICommandHandler Handler) CreateUpdateTicker(UpdateTicker command, AssetType forAssetType, AssetType domAssetType)
    {
        var webApi = webApiProvider.GetQuoteApi(forAssetType, domAssetType, false);
        var webSearchApi = webApiProvider.GetSearchApi();

        var t = webApi.GetJsonResultType();
        var tSearch = webSearchApi.GetJsonResultType();
        var commandT = (ICommand)Activator.CreateInstance(typeof(UpdateTicker<,>).MakeGenericType(t, tSearch), command);
        commandT?.StoreInLog = false;
        var handler = updateTickerHandlers.SingleOrDefault(h => h.CanHandle(commandT));
        return (commandT, handler);
    }
}