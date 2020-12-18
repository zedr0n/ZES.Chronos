/// <filename>
///     CoinMined.cs
/// </filename>

// <auto-generated/>
 namespace Chronos.Coins.Events
{
  public class CoinMined : ZES.Infrastructure.Domain.Event
  {
    public CoinMined() 
    {
    }  
    public double MineQuantity
    {
       get; 
       set;
    }  
    public CoinMined(double mineQuantity) 
    {
      MineQuantity = mineQuantity;
    }
  }
}
