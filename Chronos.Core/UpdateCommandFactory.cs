using System;
using System.Collections.Generic;
using System.Linq;
using Chronos.Core.Commands;
using Chronos.Core.Net;
using ZES.Interfaces.Domain;

namespace Chronos.Core;

/// <inheritdoc />
public class UpdateCommandFactory(ICollection<ICommandHandler<UpdateTicker>> updateTickerHandlers, IWebApiProvider webApiProvider) 
    : IUpdateCommandFactory
{
    /// <inheritdoc/>
    public (UpdateTicker Command, ICommandHandler Handler) CreateUpdateTicker(UpdateTicker command, AssetType forAssetType, AssetType domAssetType)
    {
        var webApi = webApiProvider.GetQuoteApi(forAssetType, domAssetType, false);
        var webSearchApi = webApiProvider.GetSearchApi();

        var t = webApi.GetJsonResultType();
        var tSearch = webSearchApi.GetJsonResultType();
        var commandT = (UpdateTicker)Activator.CreateInstance(typeof(UpdateTicker<,>).MakeGenericType(t, tSearch), command);
        commandT?.StoreInLog = false;
        var handler = updateTickerHandlers.SingleOrDefault(h => h.CanHandle(commandT));
        return (commandT, handler);
    }
}