using System.Threading.Tasks;
using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Core.Queries
{
    public class TransactionInfoQueryHandler : DefaultSingleQueryHandler<TransactionInfoQuery, TransactionInfo, TransactionInfo>
    {
        private readonly IQueryHandler<AssetPriceQuery, AssetPrice> _handler;
        
        public TransactionInfoQueryHandler(IProjectionManager manager, IQueryHandler<AssetPriceQuery, AssetPrice> handler)
            : base(manager)
        {
            _handler = handler;
        }

        protected override async Task<TransactionInfo> Handle(IProjection<TransactionInfo> projection, TransactionInfoQuery query)
        {
            var state = projection.State;

            if (query.Denominator == null || query.Denominator == state.Quantity.Denominator)
                return state;

            var fx = (await _handler.Handle(new AssetPriceQuery(state.Quantity.Denominator, query.Denominator)
            {
                Timestamp = query.Timestamp,
                Timeline = query.Timeline,
            })).Price;

            return new TransactionInfo(state.TxId, state.Date, new Quantity(state.Quantity.Amount * fx, query.Denominator), state.TransactionType, state.Comment);
        }
    }
}