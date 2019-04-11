using ZES.Infrastructure;
using ZES.Interfaces.Domain;

namespace Chronos.Coins.Queries
{
    public class CoinInfoQueryHandler : QueryHandler<CoinInfoQuery, CoinInfo>
    {
        private IProjection<CoinInfoProjection.StateType> _projection;
            
        public CoinInfoQueryHandler(IProjection<CoinInfoProjection.StateType>  projection)
        {
            _projection = projection;
        }
            
        public override CoinInfo Handle(CoinInfoQuery query)
        {
            return _projection.State.Get(query.Name);
        }

        public override IProjection Projection
        {
            get => _projection;
            set => _projection = value as IProjection<CoinInfoProjection.StateType>;
        }
    }
}