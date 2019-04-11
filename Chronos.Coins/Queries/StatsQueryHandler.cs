using ZES.Infrastructure;
using ZES.Interfaces;
using ZES.Interfaces.Domain;

namespace Chronos.Coins.Queries
{
    public class StatsQueryHandler : QueryHandler<StatsQuery, Stats>
    {
        private IProjection<ValueState<Stats>> _projection;

        public StatsQueryHandler(IProjection<ValueState<Stats>> projection)
        {
            _projection = projection;
        }
        
        public override Stats Handle(StatsQuery query)
        {
            return _projection.State.Value;
        }

        public override IProjection Projection
        {
            get => _projection;
            set => _projection = value as IProjection<ValueState<Stats>>;
        }
    }
}