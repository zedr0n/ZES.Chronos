using System.Collections.Generic;
using ZES.Infrastructure;

namespace Chronos.Core
{
    /// <summary>
    /// Asset quantity ( e.g. (100, "GBP"), (0.1, "Bitcoin")) 
    /// </summary>
    public record Quantity(double Amount, Asset Denominator) 
    {
    }
}