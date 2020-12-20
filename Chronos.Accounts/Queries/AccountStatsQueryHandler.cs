using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Chronos.Core;
using Chronos.Core.Queries;
using ZES.Infrastructure.Domain;
using ZES.Interfaces;
using ZES.Interfaces.Domain;

namespace Chronos.Accounts.Queries
{
    public class AccountStatsQueryHandler : QueryHandlerBase<AccountStatsQuery, AccountStats, AccountStatsState>
    {
        private readonly IQueryHandler<AssetPriceQuery, AssetPrice> _handler;
        private readonly ILog _log;
        
        public AccountStatsQueryHandler(IProjectionManager manager, IQueryHandler<AssetPriceQuery, AssetPrice> handler, ILog log) 
            : base(manager)
        {
            _handler = handler;
            _log = log;
        }

        protected override async Task<AccountStats> Handle(AccountStatsQuery query)
        {
            Projection = Manager.GetProjection<AccountStatsState>(query.Name, query.Timeline);
            await Projection.Ready;
            return await base.Handle(query);
        }

        protected override async Task<AccountStats> Handle(IProjection<AccountStatsState> projection, AccountStatsQuery query)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection), $"{typeof(IProjection<AccountStatsState>).Name}");
            var state = projection.State;
            var total = 0.0;
            foreach (var (asset, amount) in state.Assets.Zip(state.Quantities, (asset, value) => (asset, value)))
            {
                var price = 1.0;
                if (asset != query.Denominator)
                    price = (await _handler.Handle(new AssetPriceQuery(asset, query.Denominator))).Price;

                total += amount * price;
            }

            return new AccountStats(new Quantity(total, query.Denominator));
        }
    }
}