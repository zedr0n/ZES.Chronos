using System;
using System.Collections.Generic;
using ZES.Infrastructure;

namespace Chronos.Core
{
    /// <summary>
    /// Asset quantity ( e.g. (100, "GBP"), (0.1, "BTC")) 
    /// </summary>
    public record Quantity(double Amount, Asset Denominator)
    {
        public static Quantity operator +(Quantity quantity1, Quantity quantity2)
        {
            if (quantity1.Denominator != quantity2.Denominator)
                throw new InvalidOperationException("Cannot add quantities with different denominators");
                
            return new Quantity(quantity1.Amount + quantity2.Amount, quantity1.Denominator);
        }
        
        public static Quantity operator -(Quantity quantity1, Quantity quantity2)
        {
            if (quantity1.Denominator != quantity2.Denominator)
                throw new InvalidOperationException("Cannot add quantities with different denominators");
            
            return new Quantity(quantity1.Amount - quantity2.Amount, quantity1.Denominator);
        }
        
        public static Quantity operator *(Quantity quantity, double multiplier)
        {
            return new Quantity(quantity.Amount * multiplier, quantity.Denominator);
        }

        public static Quantity operator /(Quantity quantity, double divisor)
        {
            return new Quantity(quantity.Amount / divisor, quantity.Denominator);
        }
        
        /// <summary>
        /// Validates the current Quantity instance by checking whether the numeric amount is a valid number
        /// and whether the associated Denominator (Asset) is valid.
        /// </summary>
        /// <returns>True if the Quantity instance is valid, otherwise false.</returns>
        public bool IsValid() => !double.IsNaN(Amount) && Denominator.IsValid();

        /// <summary>
        /// Creates a deep copy of the current Quantity instance, maintaining the same amount and denominator.
        /// </summary>
        /// <returns>A new Quantity instance with identical properties to the current instance.</returns>
        public Quantity Copy() => new Quantity(Amount, Denominator);
    }
}