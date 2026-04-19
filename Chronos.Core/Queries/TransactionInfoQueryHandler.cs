using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.Utils;
using ZES.Interfaces.Branching;
using ZES.Interfaces.Domain;
using ZES.Interfaces.Infrastructure;

namespace Chronos.Core.Queries
{
    /// <inheritdoc />
    public class TransactionInfoQueryHandler : DefaultSingleQueryHandler<TransactionInfoQuery, TransactionInfo, TransactionInfo>
    {
        private readonly IQueryHandler<AssetQuoteQuery, AssetQuote> _handler;
        private readonly ILog _log;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionInfoQueryHandler"/> class.
        /// </summary>
        /// <param name="manager">Projection manager</param>
        /// <param name="activeTimeline">Active timeline</param>
        /// <param name="handler">Asset price handler</param>
        /// <param name="log">Log service</param>
        public TransactionInfoQueryHandler(IProjectionManager manager, ITimeline activeTimeline, IQueryHandler<AssetQuoteQuery, AssetQuote> handler, ILog log)
            : base(manager, activeTimeline)
        {
            _handler = handler;
            _log = log;
        }

        /// <inheritdoc/>
        protected override async Task<TransactionInfo> Handle(IProjection<TransactionInfo> projection, TransactionInfoQuery query)
        {
            var state = projection.State;
            if (state.TxId == null)
                return null;

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
                var denominator = state.Quotes.FirstOrDefault()?.Denominator ?? state.Quantity.Denominator;
                amount = state.Quotes.FirstOrDefault()?.Amount ?? amount;
                var fxResult = await _handler.Handle(new AssetQuoteQuery(denominator, query.Denominator)
                {
                    Timestamp = query.ConvertToDenominatorAtTxDate ? state.Date.ToTime() : query.Timestamp,
                    Timeline = query.Timeline,
                });

                if (fxResult == default)
                    throw new InvalidOperationException($"No asset price for {AssetPair.Fordom(state.Quantity.Denominator, query.Denominator)} found!");
                amount *= fxResult.Quantity.Amount;
            }
            
            return new TransactionInfo(state.TxId, state.Date, new Quantity(amount, query.Denominator), state.TransactionType, state.Comment, state.AssetId) { Quotes = new HashSet<Quantity>(state.Quotes) };
        }
    }
}