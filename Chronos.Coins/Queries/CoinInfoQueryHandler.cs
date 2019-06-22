using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Coins.Queries
{
    public class CoinInfoQueryHandler : QueryHandlerBase<CoinInfoQuery, CoinInfo, CoinInfoProjection.StateType>
    {
        public CoinInfoQueryHandler(IProjection<CoinInfoProjection.StateType> projection)
            : base(projection)
        {
        }

        protected override CoinInfo Handle(IProjection<CoinInfoProjection.StateType> projection, CoinInfoQuery query)
            => projection?.State.Get(query.Name);
    }
}