/// <filename>
///     AssetDeposited.cs
/// </filename>

// <auto-generated/>
 namespace Chronos.Accounts.Events
{
  public class AssetDeposited : ZES.Infrastructure.Domain.Event
  {
    public AssetDeposited() 
    {
    }  
    public Chronos.Core.Quantity Quantity
    {
       get; 
       set;
    }  
    public AssetDeposited(Chronos.Core.Quantity quantity) 
    {
      Quantity = quantity;
    }
  }
}

