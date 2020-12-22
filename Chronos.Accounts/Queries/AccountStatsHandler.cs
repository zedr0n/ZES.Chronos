/// <filename>
///     AccountStatsQueryHandler.cs
/// </filename>

// <auto-generated/>

using System.Transactions;
using Chronos.Accounts.Events;
using Chronos.Core.Events;

namespace Chronos.Accounts.Queries
{
  public class AccountStatsHandler : ZES.Interfaces.Domain.IProjectionHandler<AccountStatsState>
  {
    public AccountStatsState Handle (ZES.Interfaces.IEvent e, AccountStatsState state)
    {
      return Handle((dynamic) e, state);;
    }  
    public AccountStatsState Handle (Chronos.Accounts.Events.AssetDeposited e, AccountStatsState state)
    {
      var newState = new AccountStatsState(state);
      newState.Add(e.Quantity.Denominator, e.Quantity.Amount);
      return newState;
    }

    public AccountStatsState Handle(TransactionAdded e, AccountStatsState state)
    {
      var newState = new AccountStatsState(state);
      newState.Transactions.Add(e.TxId);
      return newState;
    }
  }
}

