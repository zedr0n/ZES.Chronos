using System.Collections.Generic;
using Newtonsoft.Json;
using ZES.Interfaces.Clocks;
using ZES.Interfaces.Domain;

namespace Chronos.Accounts.Queries;

[method: JsonConstructor]
public class BlendedIrr(double irr) : IHistoricalResults<BlendedIrr>
{
    public double Irr => irr;
    
    public Dictionary<Time, BlendedIrr> HistoricalResults { get; } = new();
}