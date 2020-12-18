/// <filename>
///     StatsHandler.cs
/// </filename>

// <auto-generated/>
 namespace Chronos.Accounts.Queries
{
  public class StatsHandler : ZES.Interfaces.Domain.IProjectionHandler<Stats>
  {
    public Stats Handle (ZES.Interfaces.IEvent e, Stats state)
    {
      return Handle(e as Chronos.Accounts.Events.AccountCreated, state);;
    }  
    public Stats Handle (Chronos.Accounts.Events.AccountCreated e, Stats state)
    {
      return new Stats { NumberOfAccounts = state.NumberOfAccounts + 1 };
    }
  }
}

