/// <filename>
///     TransferCoins.cs
/// </filename>

// <auto-generated/>
 namespace Chronos.Coins.Commands
{
  public class TransferCoins : ZES.Infrastructure.Domain.Command, ZES.Interfaces.Domain.ICreateCommand
  {
    public TransferCoins() 
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
    public Core.Quantity Quantity
    {
       get; 
       set;
    }  
    public Core.Quantity Fee
    {
       get; 
       set;
    }  
    public override string Target
    {
       get
      {
        return TxId;
      }
    }  
    public TransferCoins(string txId, string fromAddress, string toAddress, Core.Quantity quantity, Core.Quantity fee) 
    {
      TxId = txId; 
      FromAddress = fromAddress; 
      ToAddress = toAddress; 
      Quantity = quantity; 
      Fee = fee;
    }
  }
}

