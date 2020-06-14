using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Coins.Queries
{
    public class CoinInfoQuery : Query<CoinInfo>, ISingleQuery<CoinInfo>
    {
        public CoinInfoQuery() { }
        public CoinInfoQuery(string id)
        {
            Id = id;
        }
        
        public string Id { get; set; }
    }
}