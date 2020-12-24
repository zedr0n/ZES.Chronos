/// <filename>
///     Transaction.cs
/// </filename>

// <auto-generated/>
 namespace Chronos.Coins
{
  public sealed class Transfer : ZES.Infrastructure.Domain.AggregateRoot
  {
    public Transfer() 
    {
      Register<Chronos.Coins.Events.CoinsTransferred>(ApplyEvent);
    }  
    public Transfer(string txId, string fromAddress, string toAddress, Core.Quantity quantity, Core.Quantity fee) : this() 
    {
      When(new Chronos.Coins.Events.CoinsTransferred(txId, fromAddress, toAddress, quantity, fee));
    }  
    private void ApplyEvent (Chronos.Coins.Events.CoinsTransferred e)
    {
      Id = e.TxId;
    }
  }
}

