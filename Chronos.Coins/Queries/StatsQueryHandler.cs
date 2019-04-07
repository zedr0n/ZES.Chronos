using System.Threading.Tasks;
using ZES.Interfaces;
using ZES.Interfaces.Domain;

namespace Chronos.Coins.Queries
{
    public class StatsQueryHandler : IQueryHandler<StatsQuery, Stats>
    {
        private IProjection<ValueState<Stats>> _projection;
        private StatsProjection Projection
        {
            set => _projection = value;
        }

        public StatsQueryHandler(StatsProjection projection)
        {
            Projection = projection;
        }
        
        public Stats Handle(StatsQuery query)
        {
            return _projection.State.Value;
        }

        public Task<Stats> HandleAsync(StatsQuery query)
        {
            throw new System.NotImplementedException();
        }
    }
}