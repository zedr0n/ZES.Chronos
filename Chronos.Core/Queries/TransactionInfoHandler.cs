/// <filename>
///     TransactionInfoHandler.cs
/// </filename>

// <auto-generated/>
 namespace Chronos.Core.Queries
{
  public class TransactionInfoHandler : ZES.Interfaces.Domain.IProjectionHandler<TransactionInfo>
  {
    public TransactionInfo Handle (ZES.Interfaces.IEvent e, TransactionInfo state)
    {
      return Handle((dynamic) e, state);
    }  
    public TransactionInfo Handle (Chronos.Core.Events.TransactionRecorded e, TransactionInfo state)
    {
      state.TxId = e.TxId; 
      state.Quantity = e.Quantity;
      state.Date = e.Timestamp;
      state.TransactionType = e.TransactionType; 
      state.Comment = e.Comment; 
      return state;
    }  
    public TransactionInfo Handle (Chronos.Core.Events.TransactionDetailsUpdated e, TransactionInfo state)
    {
      state.TransactionType = e.TransactionType; 
      state.Comment = e.Comment; 
      return state;
    }
    public TransactionInfo Handle (Chronos.Core.Events.TransactionQuoteAdded e, TransactionInfo state)
    {
      state.Quotes.Add(e.Quantity);
      return state;
    }
  }
}
