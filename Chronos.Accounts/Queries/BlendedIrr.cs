using Newtonsoft.Json;

namespace Chronos.Accounts.Queries;

[method: JsonConstructor]
public class BlendedIrr(double irr)
{
    public double Irr => irr;
}