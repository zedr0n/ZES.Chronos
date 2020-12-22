/// <filename>
///     TransactionDetailsUpdated.cs
/// </filename>

// <auto-generated/>
 namespace Chronos.Core.Events
{
  public class TransactionDetailsUpdated : ZES.Infrastructure.Domain.Event
  {
    public TransactionDetailsUpdated() 
    {
    }  
    public Transaction.TransactionType TransactionType
    {
       get; 
       set;
    }  
    public string Comment
    {
       get; 
       set;
    }  
    public TransactionDetailsUpdated(Transaction.TransactionType transactionType, string comment) 
    {
      TransactionType = transactionType; 
      Comment = comment;
    }
  }
}

