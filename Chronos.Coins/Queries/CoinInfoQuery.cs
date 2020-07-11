using ZES.Infrastructure.Domain;

namespace Chronos.Coins.Queries
{
    public class CoinInfoQuery : SingleQuery<CoinInfo>
    {
        public CoinInfoQuery() { }
        public CoinInfoQuery(string name)
            : base(name)
        {
        }
    }
}