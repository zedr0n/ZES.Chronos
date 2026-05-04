using System.Collections.Generic;
using ZES.Infrastructure.Domain;

namespace Chronos.Accounts.Queries;

public class CombinedAccountStateQuery(List<string> accounts) : Query<AccountStatsState>
{
    public List<string> Accounts => accounts;
}