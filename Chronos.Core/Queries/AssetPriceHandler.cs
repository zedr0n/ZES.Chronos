/// <filename>
///     GenericAssetPriceHandler.cs
/// </filename>

// <auto-generated/>

using System;
using System.Linq;
using System.Threading.Tasks;
using ZES.Infrastructure;
using ZES.Infrastructure.Domain;
using ZES.Interfaces;
using ZES.Interfaces.Branching;
using ZES.Interfaces.Domain;

namespace Chronos.Core.Queries
{
  [Transient]
  public class AssetPriceHandler : QueryHandlerBase<AssetPriceQuery, AssetPrice, AssetPairsInfo>
  {
    private readonly IQueryHandler<SingleAssetPriceQuery, SingleAssetPrice> _handler;
    private readonly ILog _log;
    private readonly IBranchManager _branchManager;

    public AssetPriceHandler(IProjectionManager manager, ILog log, IBranchManager branchManager, IQueryHandler<SingleAssetPriceQuery, SingleAssetPrice> handler) 
      : base(manager)
    {
      _log = log;
      _branchManager = branchManager;
      _handler = handler;
    }

    protected override async Task<AssetPrice> Handle(IProjection<AssetPairsInfo> projection, AssetPriceQuery query)
    {
      var price = 1.0;
      var historical = false;
      var fordom = AssetPair.Fordom(query.ForAsset, query.DomAsset);
      var info = projection.State;
      info.Tree.Log = _log;
      var timestamp = query.Timestamp;
      if (query.Timeline != "")
        timestamp = _branchManager.GetTime(query.Timeline);
      else if (timestamp == default)
        timestamp = _branchManager.GetTime(_branchManager.ActiveBranch);
      
      if (info.Pairs.ToList().Contains(fordom))
      {
        var result = await _handler.Handle(new SingleAssetPriceQuery(fordom)
        {
          Timeline = query.Timeline,
          Timestamp = query.Timestamp,
        });
        
        if (result == null || result.Timestamp.Minus(timestamp).Days > 0 || timestamp.Minus(result.Timestamp).Days > 0)
          throw new InvalidOperationException($"Stale pricing date for {fordom}");
        price = result.Price;
      }
      else 
      {
        // try to triangulate the price
        var path = info.Tree.GetPath(query.ForAsset, query.DomAsset);
        if (path == null)
          throw new InvalidOperationException($"No path found from {query.ForAsset?.Ticker} to {query.DomAsset?.Ticker}");

        foreach (var n in path)
        {
          var pathForDom = n.forAsset + n.domAsset;
          var isInverse = info.Pairs.Contains(n.domAsset + n.forAsset);
          if (isInverse)
            pathForDom = n.domAsset + n.forAsset;

          var pathResult = await _handler.Handle(new SingleAssetPriceQuery(pathForDom)
          {
            Timeline = query.Timeline,
            Timestamp = query.Timestamp,
          });

          if (pathResult == null || pathResult.Timestamp.Minus(timestamp).Days > 0 || timestamp.Minus(pathResult.Timestamp).Days > 0)
            throw new InvalidOperationException($"Stale pricing date for {pathForDom}");

          if (isInverse)
            pathResult.Price = 1.0 / pathResult.Price;
          
          price *= pathResult.Price;
        }
      }
      
      return new AssetPrice(price, timestamp); 
    }
  }
}

