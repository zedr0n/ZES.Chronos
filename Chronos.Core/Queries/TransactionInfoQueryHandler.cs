using System.Collections.Generic;
using System.Linq;
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

            var amount = state.Quantity.Amount;

            var quote = state.Quotes.SingleOrDefault(q => q.Denominator == query.Denominator);

            if (quote != null)
            {
                amount = quote.Amount;
            }
            else
            {
                var fx = (await _handler.Handle(new AssetPriceQuery(state.Quantity.Denominator, query.Denominator)
                {
                    Timestamp = query.Timestamp,
                    Timeline = query.Timeline,
                })).Price;

                amount *= fx;
            }
            
            return new TransactionInfo(state.TxId, state.Date, new Quantity(amount, query.Denominator), state.TransactionType, state.Comment) { Quotes = new HashSet<Quantity>(state.Quotes) };
        }
    }
}