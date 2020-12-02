/// <filename>
///     CoinsTransferred.cs
/// </filename>

// <auto-generated/>
 namespace Chronos.Coins.Events
{
  public class CoinsTransferred : ZES.Infrastructure.Domain.Event
  {
    public CoinsTransferred() 
    {
    }  
    public string TxId
    {
       get; 
       set;
    }  
    public string FromAddress
    {
       get; 
       set;
    }  
    public string ToAddress
    {
       get; 
       set;
    }  
    public double Quantity
    {
       get; 
       set;
    }  
    public double Fee
    {
       get; 
       set;
    }  
    public CoinsTransferred(string txId, string fromAddress, string toAddress, double quantity, double fee) 
    {
      TxId = txId; 
      FromAddress = fromAddress; 
      ToAddress = toAddress; 
      Quantity = quantity; 
      Fee = fee;
    }
  }
}
