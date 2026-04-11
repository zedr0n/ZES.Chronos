using System.Collections.Generic;
using ZES.Infrastructure;

namespace Chronos.Core
{
    /// <summary>
    /// Asset quantity ( e.g. (100, "GBP"), (0.1, "BTC")) 
    /// </summary>
    public record Quantity(double Amount, Asset Denominator)
    {
        /// <summary>
        /// Validates the current Quantity instance by checking whether the numeric amount is a valid number
        /// and whether the associated Denominator (Asset) is valid.
        /// </summary>
        /// <returns>True if the Quantity instance is valid, otherwise false.</returns>
        public bool IsValid() => !double.IsNaN(Amount) && Denominator.IsValid();
    }
}