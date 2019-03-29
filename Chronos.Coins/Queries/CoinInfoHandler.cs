using System;
using System.Threading.Tasks;
using Chronos.Coins.Projections;
using ZES.Interfaces.Domain;

namespace Chronos.Coins.Queries
{
    public class CoinInfoHandler : IQueryHandler<CoinInfoQuery, CoinInfo>
    {
        private readonly CoinInfoProjection _coinInfoProjection;
            
        public CoinInfoHandler(CoinInfoProjection coinInfoProjection)
        {
            _coinInfoProjection = coinInfoProjection;
        }
            
        public CoinInfo Handle(CoinInfoQuery query)
        {
            var coinInfo = _coinInfoProjection.Get(query.Name);
            return coinInfo;
        }

        public Task<CoinInfo> HandleAsync(CoinInfoQuery query)
        {
            throw new NotImplementedException();
        }
    }
}