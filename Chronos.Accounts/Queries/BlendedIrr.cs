using Newtonsoft.Json;

namespace Chronos.Accounts.Queries;

[method: JsonConstructor]
public class BlendedIrr(double irr)
{
    public BlendedIrr()
        : this(0.0) { }
    public double Irr => irr;
}