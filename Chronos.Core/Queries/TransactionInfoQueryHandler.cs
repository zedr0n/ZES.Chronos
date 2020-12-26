﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using ZES.Infrastructure.Domain;
using ZES.Interfaces;
using ZES.Interfaces.Domain;
using ZES.Utils;

namespace Chronos.Core.Queries
{
    public class TransactionInfoQueryHandler : DefaultSingleQueryHandler<TransactionInfoQuery, TransactionInfo, TransactionInfo>
    {
        private readonly IQueryHandler<AssetPriceQuery, AssetPrice> _handler;
        private readonly ILog _log;
        
        public TransactionInfoQueryHandler(IProjectionManager manager, IQueryHandler<AssetPriceQuery, AssetPrice> handler, ILog log)
            : base(manager)
        {
            _handler = handler;
            _log = log;
        }

        protected override async Task<TransactionInfo> Handle(IProjection<TransactionInfo> projection, TransactionInfoQuery query)
        {
            var state = projection.State;
            if (state.TxId == null)
                return default;

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
                var fxResult = await _handler.Handle(new AssetPriceQuery(denominator, query.Denominator)
                {
                    Timestamp = query.ConvertToDenominatorAtTxDate ? state.Date : query.Timestamp,
                    Timeline = query.Timeline,
                });

                if (fxResult == default)
                    throw new InvalidOperationException($"No asset price for {AssetPair.Fordom(state.Quantity.Denominator, query.Denominator)} found!");
                amount *= fxResult.Price;
            }
            
            return new TransactionInfo(state.TxId, state.Date, new Quantity(amount, query.Denominator), state.TransactionType, state.Comment) { Quotes = new HashSet<Quantity>(state.Quotes) };
        }
    }
}