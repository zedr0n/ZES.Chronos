using Chronos.Coins.Events;
using ZES.Infrastructure.Domain;
using ZES.Interfaces;

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
        public Coin(string coinId, string ticker,string name)
        {
            base.When(new CoinCreated
            {
                CoinId = coinId,
                Name = name,
                Ticker = ticker
            });
        }

        private void When(CoinCreated e)
        {
            Id = e.CoinId;
            _ticker = e.Ticker;
            _name = e.Name;
        }
    }
}