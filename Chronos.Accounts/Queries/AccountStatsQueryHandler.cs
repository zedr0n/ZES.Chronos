using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Chronos.Core;
using Chronos.Core.Queries;
using ZES.Infrastructure;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.Utils;
using ZES.Interfaces.Domain;

namespace Chronos.Accounts.Queries
{
    public class AccountStatsQueryHandler : QueryHandlerBase<AccountStatsQuery, AccountStats, AccountStatsState>
    {
        private readonly IQueryHandler<GenericAssetPriceQuery, GenericAssetPrice> _handler;
        
        public AccountStatsQueryHandler(IProjectionManager manager, IQueryHandler<GenericAssetPriceQuery, GenericAssetPrice> handler) 
            : base(manager)
        {
            _handler = handler;
        }

        protected override async Task<AccountStats> HandleAsync(AccountStatsQuery query)
        {
            Projection = Manager.GetProjection<AccountStatsState>(query.Name, query.Timeline);
            await Projection.Ready.Timeout(Configuration.Timeout);
            return await base.HandleAsync(query);
        }

        protected override AccountStats Handle(IProjection<AccountStatsState> projection, AccountStatsQuery query)
        {
            if (projection == null)
                throw new ArgumentNullException(nameof(projection), $"{typeof(IProjection<AccountStatsState>).Name}");
            var state = projection.State;
            var total = 0.0;
            foreach (var (asset, amount) in state.Assets.Zip(state.Quantities, (asset, value) => (asset, value)))
            {
                var price = 1.0;
                if (asset != query.Denominator)
                    price = _handler.HandleAsync(new GenericAssetPriceQuery(asset, query.Denominator)).Timeout().Result.Price;

                total += amount * price;
            }

            return new AccountStats(new Quantity(total, query.Denominator));
        }
    }
}