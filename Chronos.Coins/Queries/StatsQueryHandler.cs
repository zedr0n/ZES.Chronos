using System.Threading.Tasks;
using ZES.Interfaces;
using ZES.Interfaces.Domain;

namespace Chronos.Coins.Queries
{
    public class StatsQueryHandler : IQueryHandler<StatsQuery, Stats>
    {
        private IProjection<ValueState<Stats>> _projection;

        public StatsQueryHandler(IProjection<ValueState<Stats>> projection)
        {
            _projection = projection;
        }
        
        public Stats Handle(StatsQuery query)
        {
            return _projection.State.Value;
        }

        public Task<Stats> HandleAsync(StatsQuery query)
        {
            throw new System.NotImplementedException();
        }

        public IProjection Projection
        {
            get => _projection;
            set => _projection = value as IProjection<ValueState<Stats>>;
        }
    }
}