namespace Chronos.Coins.Queries
{
    public class CoinInfo
    {
        public CoinInfo(string name, string ticker, long createdAt)
        {
            Name = name;
            Ticker = ticker;
            CreatedAt = createdAt;
        }
        
        public string Name { get; }
        public string Ticker { get; }
        public long CreatedAt { get; }
    }
}