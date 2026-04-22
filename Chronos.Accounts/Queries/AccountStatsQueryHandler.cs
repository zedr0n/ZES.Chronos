using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Chronos.Core;
using Chronos.Core.Queries;
using NodaTime;
using ZES.Infrastructure;
using ZES.Infrastructure.Domain;
using ZES.Interfaces.Branching;
using ZES.Interfaces.Domain;
using ZES.Interfaces.Infrastructure;

namespace Chronos.Accounts.Queries
{
    /// <summary>
    /// Handles the <see cref="AccountStatsQuery"/> by processing the account data and computing statistics such as positions, values, and cash balance.
    /// </summary>
    /// <remarks>
    /// This handler retrieves and transforms account-related data based on the query's specifications and projection state.
    /// </remarks>
    [Transient]
    public class AccountStatsQueryHandler : DefaultQueryHandler<AccountStatsQuery, AccountStats, AccountStatsState>
    {
        private readonly IQueryHandler<AssetQuoteQuery, AssetQuote> _handler;
        private readonly IQueryHandler<TransactionInfoQuery, TransactionInfo> _transactionInfoHandler;
        private readonly ILog _log;
        private readonly ITimeline _timeline;

        /// <summary>
        /// Handles the <see cref="AccountStatsQuery"/> by retrieving account-related data
        /// and computing statistics such as positions, account values, and cash balances.
        /// </summary>
        /// <remarks>
        /// This implementation processes the query by interacting with projection states and related handlers.
        /// </remarks>
        public AccountStatsQueryHandler(IProjectionManager manager, ITimeline activeTimeline,
            IQueryHandler<AssetQuoteQuery, AssetQuote> handler,
            IQueryHandler<TransactionInfoQuery, TransactionInfo> transactionInfoHandler, ILog log, ITimeline timeline)
            : base(manager, activeTimeline)
        {
            _handler = handler;
            _transactionInfoHandler = transactionInfoHandler;
            _log = log;
            _timeline = timeline;
        }

        protected override async Task<AccountStats> Handle(AccountStatsQuery query)
        {
            Predicate = s => (s.Type == nameof(Account) && s.SameId(query.Name)) || s.Type == nameof(AssetPair);
            return await base.Handle(query, query.Name);
        }

        private class PositionData
        {
            public Quantity Position { get; set; }
            public Quantity Value { get; set; }
            public Quantity Dividend { get; set; }
            public Quantity CostBasis { get; set; }
            public Quantity RealisedGain { get; set; }
        }

        /// <inheritdoc/>
        protected override async Task<AccountStats> Handle(IProjection<AccountStatsState> projection, AccountStatsQuery query)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection), $"{typeof(IProjection<AccountStatsState>).Name}");
            var state = projection.State;
            var total = 0.0;
            var denominator = query.Denominator;
            if (query.Denominator == null)
                denominator = state.Assets.SingleOrDefault();

            var positions = new Dictionary<string, PositionData>();
            var totalDividend = 0.0;
            //var positions = new List<Quantity>(); 
            //var values = new List<Quantity>();
            //var dividends = new List<Quantity>();

            if (query.WithPositions)
            {
                foreach (var (asset, amount) in state.Assets.Zip(state.Quantities, (asset, value) => (asset, value)))
                {
                    var price = 1.0;
                    if (asset.AssetId != denominator?.AssetId)
                    {
                        var queryResult = await _handler.Handle(new AssetQuoteQuery(asset, denominator)
                        {
                            Timestamp = query.Timestamp,
                            UpdateQuote = query.QueryNet
                        });
                        if (queryResult != null)
                            price = queryResult.Quantity.Amount;
                        else
                            return null;
                    }

                    if (asset.AssetId == null)
                        throw new InvalidOperationException("Asset id is required");
                    
                    if(!positions.ContainsKey(asset.AssetId))
                        positions[asset.AssetId] = new PositionData();

                    var positionData = positions[asset.AssetId];
                    positionData.Dividend = new Quantity(0, denominator);
                    positionData.Position = new Quantity(amount, asset);
                    positionData.Value = new Quantity(amount * price, denominator);
                    positionData.CostBasis = state.CostBasis[asset]; 
                    positionData.RealisedGain = state.RealisedGains[asset];
                    total += amount * price;
                }
            }

            // compute IRR
            var extCashflows = new List<(Instant time, double amount)>();
            
            foreach (var txId in state.Transactions)
            {
                var tx = await _transactionInfoHandler.Handle(new TransactionInfoQuery(txId, denominator)
                {
                    ConvertToDenominatorAtTxDate = query.ConvertToDenominatorAtTxDate,
                    //Timestamp = query.Timestamp,
                });
                if (tx == null)
                {
                    _log.Error($"Transaction {txId} not found");
                    return null;
                }

                if (tx.TransactionType == Transaction.TransactionType.Transfer)
                    extCashflows.Add((tx.Date, -tx.Quantity.Amount));
                
                total += tx.Quantity.Amount;
                if(tx.TransactionType == Transaction.TransactionType.Dividend)
                    totalDividend += tx.Quantity.Amount;
                
                if (!query.WithPositions || tx.TransactionType != Transaction.TransactionType.Dividend ||
                    tx.AssetId == null) continue;

                if (positions.TryGetValue(tx.AssetId, out var positionData))
                    positionData.Dividend = tx.Quantity with { Amount = positionData.Dividend.Amount + tx.Quantity.Amount };
                else
                {
                    positions[tx.AssetId] = new PositionData
                    {
                        Position = new Quantity(0, new Asset(tx.AssetId, AssetType.Equity)),
                        Value = new Quantity(0, denominator),
                        Dividend = tx.Quantity,
                        CostBasis = new Quantity(0, denominator),
                        RealisedGain = state.RealisedGains[new Asset(tx.AssetId, AssetType.Equity)], 
                    };
                }
            }
            var now = query.Timestamp?.ToInstant() ?? _timeline.Now.ToInstant();
            extCashflows.Add((now, total));

            return new AccountStats(new Quantity(total, denominator))
            {
                Positions = positions.Values.Select(p => p.Position).ToList(),
                Values = positions.Values.Select(p => p.Value).ToList(),
                Dividends = positions.Values.Select(p => p.Dividend).ToList(),
                CostBasis = positions.Values.Select(p => p.CostBasis).ToList(),
                CashBalance = new Quantity(total - positions.Values.Sum(v => v.Value.Amount), denominator),
                RealisedGains = positions.Values.Select(p => p.RealisedGain).ToList(),
                TotalDividend = new Quantity(totalDividend, denominator),
                Irr = query.ComputeIrr ? SolveIrr(extCashflows) : 0.0
            };
        }

        private static double NewtonRaphson(Func<double, (double f, double df)> func, double x0, double lower = -0.99, double upper = 10.0, double tol = 1e-8, int maxIter = 100)
        {
            var x = x0;
            for (var i = 0; i < maxIter; i++)
            {
                var (f, df) = func(x);
                if (Math.Abs(df) < 1e-12) break;
                var xNew = Math.Clamp(x - f / df, lower, upper);
                if (Math.Abs(xNew - x) < tol) return xNew;
                x = xNew;
            }
            return x;
        }        
        
        private double SolveIrr(List<(Instant time, double amount)> cashflows)
        {
            if (cashflows.Count < 2)
                return 0.0;

            var t0 = cashflows.Min(c => c.time);
            var normalised = cashflows
                .Select(c => ((c.time - t0).TotalSeconds / (365.25 * 24 * 3600), c.amount))
                .ToList();

            var years = (cashflows.Max(c => c.time) - cashflows.Min(c => c.time)).TotalSeconds / (365.25 * 24 * 3600);
            var invested = -cashflows.Where(c => c.amount < 0).Sum(c => c.amount);
            var gain = cashflows.Where(c => c.amount > 0).Sum(c => c.amount);
            var r0 = Math.Pow(gain / invested, 1.0 / years) - 1;

            return NewtonRaphson(F, r0);

            (double, double) F(double x)
            {
                var npv = 0.0;
                var dnpv = 0.0;
                foreach (var (t, cf) in normalised) {
                    var discount = Math.Pow(1 + x, t);
                    npv += cf / discount;
                    dnpv -= t * cf / (discount * (1 + x));
                }

                return (npv, dnpv);
            }
        }
    }
}