using ZES.Interfaces.Domain;

namespace Chronos.Core;

/// <summary>
/// Factory interface for creating commands and their corresponding handlers
/// to update asset quotes based on the specified parameters.
/// </summary>
public interface IUpdateQuoteCommandFactory
{
    /// <summary>
    /// Creates a command along with its corresponding handler for updating asset quotes based on the given parameters.
    /// </summary>
    /// <param name="target">The identifier of the target asset pair for which the quote is to be updated.</param>
    /// <param name="forAssetType">The type of the asset being quoted (e.g., Coin, Currency, or Equity).</param>
    /// <param name="domAssetType">The type of the domestic asset in the asset pair.</param>
    /// <param name="intraday">Specifies whether intraday quote is requested</param>
    /// <returns>A tuple consisting of the command to update the quote and the handler responsible for executing the command.</returns>
    (ICommand Command, ICommandHandler Handler) Create(string target, AssetType forAssetType, AssetType domAssetType, bool intraday);
}