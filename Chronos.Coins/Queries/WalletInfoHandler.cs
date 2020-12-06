/// <filename>
///     WalletInfoQueryHandler.cs
/// </filename>

// <auto-generated/>
 namespace Chronos.Coins.Queries
{
  public class WalletInfoHandler : ZES.Interfaces.Domain.IProjectionHandler<WalletInfo>
  {
    public WalletInfo Handle (ZES.Interfaces.IEvent e, WalletInfo state)
    {
      return Handle((dynamic) e, state); 
    }  
    public WalletInfo Handle (Chronos.Coins.Events.WalletCreated e, WalletInfo state)
    {
      state.Address = e.Address; 
      return state;
    }  
    public WalletInfo Handle (Chronos.Coins.Events.WalletBalanceChanged e, WalletInfo state)
    {
      return new WalletInfo
      {
        Address = state.Address,
        Balance = state.Balance + e.Delta,
      };
    }
    public WalletInfo Handle (Chronos.Coins.Events.CoinMined e, WalletInfo state)
    {
      state.MineQuantity = e.MineQuantity; 
      return state;
    }
  }
}

