using ZES.Interfaces.Domain;

namespace Chronos.Core;

/// <summary>
/// Represents a factory for creating update ticker commands and their associated handlers.
/// </summary>
public interface IUpdateTickerCommandFactory
{
    /// <summary>
    /// Creates a command and its associated handler for updating a ticker based on the specified target and asset types.
    /// </summary>
    /// <param name="target">The target identifier for which the ticker update is to be performed.</param>
    /// <param name="forAssetType">The asset type representing the foreign asset.</param>
    /// <param name="domAssetType">The asset type representing the domestic asset.</param>
    /// <returns>A tuple containing the command to update the ticker and its associated handler.</returns>
    (ICommand Command, ICommandHandler Handler) Create(string target, AssetType forAssetType, AssetType domAssetType);
}