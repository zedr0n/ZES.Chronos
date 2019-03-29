using ZES.Infrastructure.Domain;

namespace Chronos.Coins.Events
{
    public class CoinCreated : Event
    {
        public string Name { get; set; }
        public string Ticker { get; set; }

        public CoinCreated(string name, string ticker)
        {
            Name = name;
            Ticker = ticker;
            EventType = nameof(CoinCreated);
        }
    }
}