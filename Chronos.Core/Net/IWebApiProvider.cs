using ZES.Interfaces.Net;

namespace Chronos.Core.Net;

/// <summary>
/// Represents a provider interface for accessing various Web API services.
/// </summary>
public interface IWebApiProvider
{
    /// <summary>
    /// Retrieves an implementation of the <see cref="IWebQuoteApi"/> interface capable of handling the specified asset types.
    /// </summary>
    /// <param name="forAssetType">The <see cref="AssetType"/> representing the type of the foreign asset for which the API is to be retrieved.</param>
    /// <param name="domAssetType">The <see cref="AssetType"/> representing the type of the domestic asset associated with the requested API.</param>
    /// <param name="intraday">A boolean value indicating whether the API should support intraday data.</param>
    /// <returns>An instance of <see cref="IWebQuoteApi"/> that can handle the specified asset types, or null if no suitable implementation is found.</returns>
    IWebQuoteApi GetQuoteApi(AssetType forAssetType, AssetType domAssetType, bool intraday);

    /// <summary>
    /// Retrieves the web search API for ticker and exchange lookups.
    /// </summary>
    /// <returns>An instance of <see cref="IWebSearchApi"/> for performing ticker searches.</returns>
    IWebSearchApi GetSearchApi();
}