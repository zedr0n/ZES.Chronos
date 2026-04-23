using System.Collections.Generic;
using Chronos.Core;
using Newtonsoft.Json;
using NodaTime;
using ZES.Infrastructure.Domain;
using ZES.Interfaces.Clocks;

namespace Chronos.Accounts.Queries;

[method: JsonConstructor]
public class BlendedIrrQuery(List<string> accounts, Asset denominator) : Query<BlendedIrr>
{
    public bool QueryNet { get; set; }
    public List<string> Accounts => accounts;
    public Asset Denominator => denominator;
    public Instant Start { get; set; }
}