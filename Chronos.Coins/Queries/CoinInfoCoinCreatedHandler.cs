using Chronos.Coins.Events;
using ZES.Infrastructure.Projections;

namespace Chronos.Coins.Queries
{
    public class CoinInfoCoinCreatedHandler : ProjectionHandlerBase<CoinInfo, CoinCreated>
    {
        public override CoinInfo Handle(CoinCreated e, CoinInfo state)
        {
            state.Name = e.Name;
            state.Ticker = e.Ticker;
            state.CreatedAt = e.Timestamp;
            return state;
        }
    }
}