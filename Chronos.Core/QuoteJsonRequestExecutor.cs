using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Chronos.Core.Net;
using ZES.Infrastructure;
using ZES.Infrastructure.Alerts;
using ZES.Infrastructure.Net;
using ZES.Interfaces.Domain;
using ZES.Interfaces.Infrastructure;

namespace Chronos.Core;

/// <inheritdoc />
public class QuoteJsonRequestExecutor<T>(IMessageQueue messageQueue, ICommandHandler<RequestJson<T>> requestHandler) : IQuoteJsonRequestExecutor<T>
    where T : class, IWebQuoteJsonResult
{
    /// <inheritdoc/>
    public Type JsonResultType => typeof(T);

    /// <inheritdoc/>
    public async Task<IWebQuoteJsonResult> Request(string requestorId, string url)
    {
        var obs = messageQueue.Alerts.OfType<JsonRequestCompleted<T>>().Replay();
        obs.Connect();
        
        await requestHandler.Handle(new RequestJson<T>(requestorId, url));
        var res = await obs.FirstOrDefaultAsync(r => r.RequestorId == requestorId).Timeout(Configuration.Timeout);
        return res.Data;
    }
}