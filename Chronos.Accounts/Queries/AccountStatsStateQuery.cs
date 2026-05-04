using ZES.Infrastructure.Domain;

namespace Chronos.Accounts.Queries;

public class AccountStatsStateQuery(string account) : Query<AccountStatsState>
{
    public string Account => account;
}