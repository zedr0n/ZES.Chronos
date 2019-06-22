using System.Collections.Generic;
using ZES.Infrastructure;

namespace Chronos.Core
{
    public class BookValue : ValueObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BookValue"/> class.
        /// </summary>
        /// <param name="denominator">Denominator asset</param>
        /// <param name="value">Value denominated in the asset</param>
        public BookValue(Asset denominator, double value)
        {
            Denominator = denominator;
            Value = value;
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
        public double Value { get; private set; }
        
        /// <inheritdoc />
        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Denominator;
        }
    }
}