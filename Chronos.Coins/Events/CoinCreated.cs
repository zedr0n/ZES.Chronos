using ZES.Infrastructure.Domain;

namespace Chronos.Coins.Events
{
    public class CoinCreated : Event
    {
        public CoinCreated(string name, string ticker)
        {
            Name = name;
            Ticker = ticker;
        }
        
        public string Name { get; }
        public string Ticker { get; }
    }
}