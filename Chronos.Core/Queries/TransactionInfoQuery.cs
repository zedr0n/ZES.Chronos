/// <filename>
///     TransactionInfoQuery.cs
/// </filename>

// <auto-generated/>
 namespace Chronos.Core.Queries
{
  public class TransactionInfoQuery : ZES.Infrastructure.Domain.SingleQuery<TransactionInfo>
  {
    public string TxId
    {
       get; 
       set;
    }  
    public Core.Asset Denominator
    {
       get; 
       set;
    }  
    public TransactionInfoQuery() 
    {
    }  
    public TransactionInfoQuery(string txId, Core.Asset denominator = null) : base(txId) 
    {
      TxId = txId; 
      Denominator = denominator;
    }
  }
}
