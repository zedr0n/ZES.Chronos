using System.Collections.Generic;
using ZES.Infrastructure;

namespace Chronos.Core
{
    /// <summary>
    /// Asset quantity ( e.g. (100, "GBP"), (0.1, "Bitcoin")) 
    /// </summary>
    public class Quantity : ValueObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Quantity"/> class.
        /// </summary>
        /// <param name="amount">Value denominated in the asset</param>
        /// <param name="denominator">Denominator asset</param>
        public Quantity(double amount, Asset denominator)
        {
            Denominator = denominator;
            Amount = amount;
        }

        /// <summary>
        /// Gets denominator asset
        /// </summary>
        /// <value>
        /// Denominator asset
        /// </value>
        public Asset Denominator { get; private set; }

        /// <summary>
        /// Gets value denominated in the denominator asset
        /// </summary>
        /// <value>
        /// Value denominated in the denominator asset
        /// </value>
        public double Amount { get; private set; }
        
        /// <inheritdoc />
        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Denominator;
            yield return Amount;
        }
    }
}