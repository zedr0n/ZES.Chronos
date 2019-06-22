using ZES.Infrastructure.Domain;

namespace Chronos.Coins.Queries
{
    public class CoinInfoQuery : Query<CoinInfo> 
    {
        public CoinInfoQuery() { }
        public CoinInfoQuery(string name)
        {
            Name = name;
        }
        
        public string Name { get; set; }
    }
}