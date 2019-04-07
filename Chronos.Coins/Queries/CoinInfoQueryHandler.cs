using System;
using System.Threading.Tasks;
using ZES.Interfaces.Domain;

namespace Chronos.Coins.Queries
{
    public class CoinInfoQueryHandler : IQueryHandler<CoinInfoQuery, CoinInfo>
    {
        private readonly CoinInfoProjection _coinInfoProjection;
            
        public CoinInfoQueryHandler(CoinInfoProjection coinInfoProjection)
        {
            _coinInfoProjection = coinInfoProjection;
        }
            
        public CoinInfo Handle(CoinInfoQuery query)
        {
            var coinInfo = _coinInfoProjection.State.Get(query.Name);
            return coinInfo;
        }

        public Task<CoinInfo> HandleAsync(CoinInfoQuery query)
        {
            throw new NotImplementedException();
        }
    }
}