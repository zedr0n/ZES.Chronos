using System.Collections.Generic;
using Chronos.Core;
using Newtonsoft.Json;
using ZES.Infrastructure.Domain;

namespace Chronos.Accounts.Queries;

[method: JsonConstructor]
public class CombinedAccountStatsQuery(List<string> accounts, Asset denominator) : Query<AccountStats>
{
    public Asset Denominator => denominator;
    public List<string> Accounts => accounts;
    public bool QueryNet { get; set; }
    public int NumberOfMatchingDays { get; set; } = 30;
}