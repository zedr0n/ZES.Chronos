namespace Chronos.Coins.Queries
{
    public class CoinInfo
    {
        public CoinInfo(string name, string ticker)
        {
            Name = name;
            Ticker = ticker;
        }
        
        public string Name { get; }
        public string Ticker { get; }
    }
}