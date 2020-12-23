/// <filename>
///     StartTransfer.cs
/// </filename>

// <auto-generated/>
 namespace Chronos.Accounts.Commands
{
  public class StartTransfer : ZES.Infrastructure.Domain.Command, ZES.Interfaces.Domain.ICreateCommand
  {
    public StartTransfer() 
    {
    }  
    public string TxId
    {
       get; 
       set;
    }  
    public string FromAccount
    {
       get; 
       set;
    }  
    public string ToAccount
    {
       get; 
       set;
    }  
    public Core.Quantity Amount
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
    public StartTransfer(string txId, string fromAccount, string toAccount, Core.Quantity amount) 
    {
      TxId = txId; 
      FromAccount = fromAccount; 
      ToAccount = toAccount; 
      Amount = amount;
    }
  }
}
