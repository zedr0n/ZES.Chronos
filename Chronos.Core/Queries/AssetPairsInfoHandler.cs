/// <filename>
///     AssetPairsInfoHandler.cs
/// </filename>

// <auto-generated/>

using System.Collections.Generic;
using System.Linq;

namespace Chronos.Core.Queries
{
  public class AssetPairsInfoHandler : ZES.Interfaces.Domain.IProjectionHandler<AssetPairsInfo>
  {
    public AssetPairsInfo Handle (ZES.Interfaces.IEvent e, AssetPairsInfo state)
    {
      return Handle(e as Chronos.Core.Events.AssetPairRegistered, state);
    }  
    public AssetPairsInfo Handle (Chronos.Core.Events.AssetPairRegistered e, AssetPairsInfo state)
    {
      var pairs = new HashSet<string>(state.Pairs) { e.Fordom };
      var assets = new HashSet<Asset>(state.Tree.Assets) {e.DomAsset, e.ForAsset};
      var newState = new AssetPairsInfo(assets.ToArray(),pairs.ToArray(), state.Tree);
      newState.Tree.Add(e.ForAsset, e.DomAsset);
      return newState;
    }
  }
}

