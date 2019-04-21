using ZES.Infrastructure;

namespace Chronos.Coins.Queries
{
    public class CoinInfoQuery : Query<CoinInfo> 
    {
        public string Name { get; set; }

        public CoinInfoQuery() {}
        public CoinInfoQuery(string name)
        {
            Name = name;
        }
    }
}