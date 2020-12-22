/// <filename>
///     TransactionListQuery.cs
/// </filename>

// <auto-generated/>
 namespace Chronos.Accounts.Queries
{
  public class TransactionListQuery : ZES.Infrastructure.Domain.SingleQuery<TransactionList>
  {
    public string Name
    {
       get; 
       set;
    }  
    public TransactionListQuery() 
    {
    }  
    public TransactionListQuery(string name) : base(name) 
    {
      Name = name;
    }
  }
}

