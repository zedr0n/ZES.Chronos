using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Chronos.Core;
using Chronos.Core.Queries;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.Utils;
using ZES.Interfaces.Domain;

namespace Chronos.Accounts.Queries
{
    public class AccountStatsQueryHandler : QueryHandlerBase<AccountStatsQuery, AccountStats, AccountStatsState>
    {
        private readonly IQueryHandler<AssetPriceQuery, AssetPrice> _handler;
        private readonly IQueryHandler<AssetPairsInfoQuery, AssetPairsInfo> _allPairsHandler;
        
        public AccountStatsQueryHandler(IProjectionManager manager, IQueryHandler<AssetPriceQuery, AssetPrice> handler, IQueryHandler<AssetPairsInfoQuery, AssetPairsInfo> allPairsHandler) 
            : base(manager)
        {
            _handler = handler;
            _allPairsHandler = allPairsHandler;
        }

        protected override async Task<AccountStats> HandleAsync(AccountStatsQuery query)
        {
            Projection = Manager.GetProjection<AccountStatsState>(query.Name, query.Timeline);
            await Projection.Ready;
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
                {
                    var info = _allPairsHandler.HandleAsync(new AssetPairsInfoQuery()).Result;
                    if (info.Pairs.Contains(AssetPair.Fordom(asset, query.Denominator)))
                    {
                        price = _handler.HandleAsync(new AssetPriceQuery(AssetPair.Fordom(asset, query.Denominator)))
                            .Timeout()
                            .Result
                            .Price.Amount;
                    }
                    else 
                    {
                        // try to triangulate the price
                        var path = info.Tree.GetPath(asset, query.Denominator);
                        if (path == null)
                            throw new InvalidOperationException($"No path found from {asset.AssetId} to {query.Denominator.AssetId}");
                        price = 1.0;
                        foreach (var fordom in path)
                        {
                            var pathPrice = _handler.HandleAsync(new AssetPriceQuery(fordom)).Timeout().Result;
                            price *= pathPrice.Price.Amount;
                        }
                    }
                }

                total += amount * price;
            }

            return new AccountStats(new Quantity(total, query.Denominator));
        }
    }
}