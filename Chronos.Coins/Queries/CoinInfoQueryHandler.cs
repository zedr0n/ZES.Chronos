using System;
using System.Threading.Tasks;
using ZES.Interfaces.Domain;

namespace Chronos.Coins.Queries
{
    public class CoinInfoQueryHandler : IQueryHandler<CoinInfoQuery, CoinInfo>
    {
        private IProjection<CoinInfoProjection.StateType> _projection;
            
        public CoinInfoQueryHandler(IProjection<CoinInfoProjection.StateType>  projection)
        {
            _projection = projection;
        }
            
        public CoinInfo Handle(CoinInfoQuery query)
        {
            var coinInfo = _projection.State.Get(query.Name);
            return coinInfo;
        }

        public Task<CoinInfo> HandleAsync(CoinInfoQuery query)
        {
            throw new NotImplementedException();
        }

        public IProjection Projection
        {
            get => _projection;
            set => _projection = value as IProjection<CoinInfoProjection.StateType>;
        }
    }
}