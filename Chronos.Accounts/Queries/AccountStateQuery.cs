using ZES.Infrastructure.Domain;

namespace Chronos.Accounts.Queries;

public class AccountStateQuery(string account) : Query<AccountState>
{
    public string Account => account;
}