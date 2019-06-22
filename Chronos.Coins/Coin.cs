using Chronos.Coins.Events;
using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Coins
{
    /// <summary>
    /// Crypto coin aggregate root
    /// </summary>
    public class Coin : EventSourced, IAggregate 
    {
        private string _ticker;
        private string _name;

        public Coin()
        {
            Register<CoinCreated>(When);
        }
        
        public Coin(string ticker, string name)
            : this()
        {
            base.When(new CoinCreated(name, ticker));
        }

        private void When(CoinCreated e)
        {
            Id = e.Name;
            _ticker = e.Ticker;
            _name = e.Name;
        }
    }
}