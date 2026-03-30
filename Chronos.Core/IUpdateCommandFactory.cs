using Chronos.Core.Commands;
using ZES.Interfaces.Domain;

namespace Chronos.Core;

/// <summary>
/// Factory interface for creating commands and their corresponding handlers
/// to update asset quotes based on the specified parameters.
/// </summary>
public interface IUpdateCommandFactory
{
    /// <summary>
    /// Creates a command along with its corresponding handler for updating asset quotes based on the provided parameters.
    /// </summary>
    /// <param name="command">The command specifying the parameters for updating the asset quote.</param>
    /// <param name="forAssetType">The type of the asset being quoted (e.g., Coin, Currency, or Equity).</param>
    /// <param name="domAssetType">The type of the domestic asset in the asset pair.</param>
    /// <param name="intraday">Indicates whether the requested quote is intraday.</param>
    /// <returns>A tuple containing the command to update the quote and the handler responsible for executing the command.</returns>
    (ICommand Command, ICommandHandler Handler) CreateUpdateQuote(UpdateQuote command, AssetType forAssetType, AssetType domAssetType, bool intraday);

    /// <summary>
    /// Creates a command along with its corresponding handler for updating asset tickers based on the given parameters.
    /// </summary>
    /// <param name="command">The command instance containing details for updating the ticker.</param>
    /// <param name="forAssetType">The type of the asset being quoted (e.g., Coin, Currency, or Equity).</param>
    /// <param name="domAssetType">The type of the domestic asset in the asset pair.</param>
    /// <returns>A tuple consisting of the command to update the ticker and the handler responsible for executing the command.</returns>
    (ICommand Command, ICommandHandler Handler) CreateUpdateTicker(UpdateTicker command, AssetType forAssetType, AssetType domAssetType);
}