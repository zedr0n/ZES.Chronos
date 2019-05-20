using ZES.Infrastructure;
using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Coins.Queries
{
    public class CoinInfoQueryHandler : QueryHandler<CoinInfoQuery, CoinInfo>
    {
        private IProjection<CoinInfoProjection.StateType> _projection;
            
        public CoinInfoQueryHandler(IProjection<CoinInfoProjection.StateType> projection)
        {
            _projection = projection;
        }

        protected override IProjection Projection => _projection;

        public override CoinInfo Handle(IProjection projection, CoinInfoQuery query)
        {
            return (projection as IProjection<CoinInfoProjection.StateType>)?.State.Get(query.Name);
        }
    }
}