using ZES.Interfaces.Domain;

namespace Chronos.Coins.Queries
{
    public class CoinInfo : ISingleState
    {
        public CoinInfo() { }
        public CoinInfo(string name, string ticker, long createdAt)
        {
            Name = name;
            Ticker = ticker;
            CreatedAt = createdAt;
        }
        
        public string Name { get; set; }
        public string Ticker { get; set; }
        public long CreatedAt { get; set; }
    }
}