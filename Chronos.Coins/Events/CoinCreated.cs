using ZES.Infrastructure.Domain;

namespace Chronos.Coins.Events
{
    public class CoinCreated : Event
    {
        public string CoinId { get; set; }
        public string Ticker { get; set; }
        public string Name { get; set; }

        public CoinCreated(string name, string ticker)
        {
            CoinId = Name = name;
            Ticker = ticker;
            EventType = nameof(CoinCreated);
        }
    }
}