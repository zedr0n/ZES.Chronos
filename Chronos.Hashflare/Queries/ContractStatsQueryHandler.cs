using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Hashflare.Queries
{
    /// <inheritdoc />
    public class ContractStatsQueryHandler : QueryHandlerBaseEx<ContractStatsQuery, ContractStats, ContractStatsProjection.Results>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContractStatsQueryHandler"/> class.
        /// </summary>
        /// <param name="projection">Underlying projectior</param>
        public ContractStatsQueryHandler(IProjection<ContractStatsProjection.Results> projection) 
            : base(projection)
        {
        }

        /// <inheritdoc />
        protected override ContractStats Handle(IProjection<ContractStatsProjection.Results> projection, ContractStatsQuery query)
         => new ContractStats(query.ContractId, projection.State.Type(query.ContractId), projection.State.Mined(query.ContractId));
    }
}