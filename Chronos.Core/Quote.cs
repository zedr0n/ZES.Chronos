using System;
using System.Collections.Generic;
using NodaTime;
using ZES.Infrastructure;

#pragma warning disable 0660
#pragma warning disable 0661

namespace Chronos.Core
{
    /// <summary>
    /// Asset price quote
    /// </summary>
    public class Quote : ValueObject, IEquatable<Quote>
    {
        private double _low;
        private double _high;
        private double _open;

        /// <summary>
        /// Initializes a new instance of the <see cref="Quote"/> class.
        /// </summary>
        /// <param name="date">Quote date</param>
        /// <param name="close">Close price</param>
        public Quote(Instant date, double close)
        {
            Date = date;
            Close = close;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Quote"/> class.
        /// </summary>
        /// <param name="date">Quote date</param>
        /// <param name="close">Close price</param>
        /// <param name="open">Open price</param>
        /// <param name="high">High price</param>
        /// <param name="low">Low price</param>
        public Quote(Instant date, double close, double open, double high, double low)
        {
            Date = date;
            Close = close;
            Open = open;
            High = high;
            Low = low;
        }
       
        /// <summary>
        /// Gets quote timestamp
        /// </summary>
        public Instant Date { get; }
        
        /// <summary>
        /// Gets close price
        /// </summary>
        public double Close { get; }

        /// <summary>
        /// Gets or sets open price
        /// </summary>
        public double Open
        {
            get => _open == 0 ? Close : _open;
            set => _open = value;
        }

        /// <summary>
        /// Gets or sets high price
        /// </summary>
        public double High
        {
            get => _high == 0 ? Close : _high;
            set => _high = value;
        }

        /// <summary>
        /// Gets or sets low price
        /// </summary>
        public double Low
        {
            get => _low == 0 ? Close : _low;
            set => _low = value;
        }

        /// <summary>
        /// Equal operator
        /// </summary>
        /// <param name="left">Left instance</param>
        /// <param name="right">Right instance</param>
        /// <returns>True if equal</returns>
        public static bool operator ==(Quote left, Quote right)
        {
            return EqualOperator(left, right);
        }

        /// <summary>
        /// Not equal operator
        /// </summary>
        /// <param name="left">Left instance</param>
        /// <param name="right">Right instance</param>
        /// <returns>True if not equal</returns>
        public static bool operator !=(Quote left, Quote right)
        {
            return NotEqualOperator(left, right);
        }

        /// <inheritdoc />
        public bool Equals(Quote other)
        {
            return base.Equals(other);
        }

        /// <inheritdoc />
        protected override IEnumerable<object> GetAtomicValues()
        {
            yield return Date;
            yield return Open;
            yield return Close;
            yield return High;
            yield return Low;
        }
    }
}