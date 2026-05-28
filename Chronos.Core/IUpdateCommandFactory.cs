using Chronos.Core.Commands;
using ZES.Infrastructure.Net;
using ZES.Interfaces.Domain;

namespace Chronos.Core;

/// <summary>
/// Factory interface for creating commands and their corresponding handlers
/// to update asset quotes based on the specified parameters.
/// </summary>
public interface IUpdateCommandFactory
{
    /// <summary>
    /// Creates a command along with its corresponding handler for updating asset tickers based on the given parameters.
    /// </summary>
    /// <param name="command">The command instance containing details for updating the ticker.</param>
    /// <param name="forAssetType">The type of the asset being quoted (e.g., Coin, Currency, or Equity).</param>
    /// <param name="domAssetType">The type of the domestic asset in the asset pair.</param>
    /// <returns>A tuple consisting of the command to update the ticker and the handler responsible for executing the command.</returns>
    (UpdateTicker Command, ICommandHandler Handler) CreateUpdateTicker(UpdateTicker command, AssetType forAssetType, AssetType domAssetType);
}