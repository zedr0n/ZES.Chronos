using Chronos.Coins.Events;
using ZES.Infrastructure.Projections;

namespace Chronos.Coins.Queries
{
    public class StatsHandler : ProjectionHandlerBase<Stats, CoinCreated>
    {
        public override Stats Handle(CoinCreated e, Stats state)
        {
            state.Increment();
            return state;
        }
    }
}