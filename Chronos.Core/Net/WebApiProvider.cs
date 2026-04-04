using System.Collections.Generic;
using System.Linq;

namespace Chronos.Core.Net;

/// <summary>
/// Provides access to web API implementations for quotes and searches.
/// </summary>
public class WebApiProvider(IEnumerable<IWebQuoteApi> apis, IWebSearchApi searchApi) : IWebApiProvider
{
    /// <inheritdoc/>
    public IWebQuoteApi GetQuoteApi(AssetType forAssetType, AssetType domAssetType, bool intraday)
    {
        var api = apis.SingleOrDefault(x => x.CanHandle(forAssetType, domAssetType, intraday));
        return api;
    }

    /// <inheritdoc/>
    public IWebSearchApi GetSearchApi()
    {
        return searchApi;
    }
}