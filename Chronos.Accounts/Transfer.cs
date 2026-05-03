 using Chronos.Accounts.Events;
 using Chronos.Core;
 using ZES.Infrastructure.Domain;

 namespace Chronos.Accounts;
 
 public sealed class Transfer : AggregateRoot
 {
     public Transfer() 
     {
         Register<TransferStarted>(ApplyEvent);
     }  
     public Transfer(string txId, string fromAccount, string toAccount, Quantity amount, Quantity fee) : this() 
     {
         When(new TransferStarted(txId, fromAccount, toAccount, amount, fee));
     }  
     private void ApplyEvent (TransferStarted e)
     {
         Id = e.TxId;
     }
 }

