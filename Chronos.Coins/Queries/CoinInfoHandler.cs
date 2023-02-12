/// <filename>
///     CoinInfoHandler.cs
/// </filename>

// <auto-generated/>

using Chronos.Core;

namespace Chronos.Coins.Queries
{
  public class CoinInfoHandler : ZES.Interfaces.Domain.IProjectionHandler<CoinInfo>
  {
    public CoinInfo Handle (ZES.Interfaces.IEvent e, CoinInfo state)
    {
      return Handle(e as Chronos.Coins.Events.CoinCreated, state);;
    }  
    public CoinInfo Handle (Chronos.Coins.Events.CoinCreated e, CoinInfo state)
    {
      state.Name = e.Name; 
      state.Ticker = e.Ticker;
      state.Asset = new Asset(e.Name, e.Ticker, AssetType.Coin);
      return state;
    }
  }
}

