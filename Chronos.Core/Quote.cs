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
    public record Quote(Instant Date, double Close, double Open, double High, double Low) 
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Quote"/> class.
        /// </summary>
        /// <param name="date">Quote date</param>
        /// <param name="close">Close price</param>
        public Quote(Instant date, double close)
            : this(date, close, close, close, close)
        {
        }
    }
}