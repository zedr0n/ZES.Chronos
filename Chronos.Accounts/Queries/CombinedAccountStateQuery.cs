using System.Collections.Generic;
using ZES.Infrastructure.Domain;

namespace Chronos.Accounts.Queries;

public class CombinedAccountStateQuery(List<string> accounts) : Query<AccountState>
{
    public List<string> Accounts => accounts;
}