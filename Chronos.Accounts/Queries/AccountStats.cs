/// <filename>
///     AccountStats.cs
/// </filename>

// <auto-generated/>
 namespace Chronos.Accounts.Queries
{
  public class AccountStats : ZES.Interfaces.Domain.IState
  {
    public AccountStats() 
    {
    }  
    public int NumberOfAccounts
    {
       get; 
       set;
    }  
    public AccountStats(int numberOfAccounts) 
    {
      NumberOfAccounts = numberOfAccounts;
    }
  }
}

