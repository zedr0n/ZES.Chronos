/// <filename>
///     WalletInfoQuery.cs
/// </filename>

// <auto-generated/>
 namespace Chronos.Coins.Queries
{
  public class WalletInfoQuery : ZES.Infrastructure.Domain.SingleQuery<WalletInfo>
  {
    public string Address
    {
       get; 
       set;
    }  
    public WalletInfoQuery() 
    {
    }  
    public WalletInfoQuery(string address) : base(address) 
    {
      Address = address;
    }
  }
}

