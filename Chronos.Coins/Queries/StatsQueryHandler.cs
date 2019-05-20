using ZES.Infrastructure;
using ZES.Infrastructure.Domain;
using ZES.Interfaces;
using ZES.Interfaces.Domain;

namespace Chronos.Coins.Queries
{
    public class StatsQueryHandler : QueryHandler<StatsQuery, Stats>
    {
        private readonly IProjection<ValueState<Stats>> _projection;

        public StatsQueryHandler(IProjection<ValueState<Stats>> projection)
        {
            _projection = projection;
        }

        protected override IProjection Projection => _projection;

        public override Stats Handle(IProjection projection, StatsQuery query)
        {
            return (projection as IProjection<ValueState<Stats>>)?.State.Value;
        }
    }
}