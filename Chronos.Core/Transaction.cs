/// <filename>
///     Transaction.cs
/// </filename>

// <auto-generated/>
 namespace Chronos.Core
{
  public sealed class Transaction : ZES.Infrastructure.Domain.AggregateRoot
  {
    public Transaction() 
    {
      Register<Chronos.Core.Events.TransactionRecorded>(ApplyEvent); 
      Register<Chronos.Core.Events.TransactionDetailsUpdated>(ApplyEvent);
    }  
    public Transaction(string txId, Quantity quantity, Transaction.TransactionType transactionType, string comment) : this() 
    {
      When(new Chronos.Core.Events.TransactionRecorded(txId, quantity, transactionType, comment));
    }  

    public enum TransactionType
    {
      Buy,
      Sell,
      Income,
      Spend,
      Transfer,
    }
    
    public void UpdateDetails (Transaction.TransactionType transactionType, string comment)
    {
      When(new Chronos.Core.Events.TransactionDetailsUpdated(transactionType, comment));
    }  
    private void ApplyEvent (Chronos.Core.Events.TransactionRecorded e)
    {
      Id = e.TxId;
    }  
    private void ApplyEvent (Chronos.Core.Events.TransactionDetailsUpdated e)
    {
    }
  }
}

