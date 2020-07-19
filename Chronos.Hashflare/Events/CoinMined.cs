using ZES.Infrastructure.Domain;

namespace Chronos.Hashflare.Events
{
    /// <inheritdoc />
    public class CoinMined : Event
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CoinMined"/> class.
        /// </summary>
        public CoinMined() { }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="CoinMined"/> class.
        /// </summary>
        /// <param name="type">Coin type</param>
        /// <param name="quantity">Coin quantity</param>
        public CoinMined(string type, double quantity)
        {
            Type = type;
            Quantity = quantity;
        }

        /// <summary>
        /// Gets mined coin type
        /// </summary>
        public string Type { get; internal set; }
        
        /// <summary>
        /// Gets quantity of coin mined
        /// </summary>
        public double Quantity { get; internal set; }
    }
}