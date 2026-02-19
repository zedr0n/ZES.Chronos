using System;
using System.Linq;
using System.Reflection;
using Chronos.Coins.Commands;
using Chronos.Coins.Queries;
using Chronos.Core;
using Chronos.Core.Queries;
using SimpleInjector;
using ZES.Infrastructure;
using ZES.Infrastructure.Domain;
using ZES.Infrastructure.GraphQl;
using ZES.Infrastructure.Utils;
using ZES.Interfaces.Branching;
using ZES.Interfaces.Infrastructure;
using ZES.Utils;

#pragma warning disable SA1600

namespace Chronos.Coins
{
    public static class Config
    {
        [Registration]
        public static void RegisterAll(Container c)
        {
            c.RegisterAll(Assembly.GetExecutingAssembly());
        }
        
        public class Query : GraphQlQuery
        {
            public Query(IBus bus)
                : base(bus)
            {
            }

            public CoinInfo CoinInfo(string name) => Resolve(new CoinInfoQuery(name));
            public Stats Stats(string date = null) => Resolve(new StatsQuery { Timestamp = date.ToTime() });
            public WalletInfo WalletInfo(string address, string date = null) => Resolve(new WalletInfoQuery(address) { Timestamp = date.ToTime() });
        }

        public class Mutation : GraphQlMutation
        {
            private readonly IBus _bus;

            public Mutation(IBus bus, ILog log, IBranchManager manager)
                : base(bus, log, manager)
            {
                _bus = bus;
            }

            public bool UpdateDailyOutflow(string address, int index, bool? useRemote = null, bool? useV2 = null, int? count = null) => Resolve(new UpdateDailyOutflow(address, index)
            {
                UseRemote = useRemote ?? false,
                UseV2 = useV2 ?? false,
                Count = count ?? 1000,
            });

            public bool UpdateDailyMining(string address, int index, bool? useRemote = null, bool? useV2 = null, int? count = null)
            {
                var command = new UpdateDailyMining(address, index)
                {
                    UseRemote = useRemote ?? false,
                    UseV2 = useV2 ?? false, 
                    Count = count ?? 1000,
                };
                return Resolve(command);
            }

            public bool CreateCoin(string coin, string ticker) => Resolve(new CreateCoin(coin, ticker));

            public bool CreateWallet(string address, string coinId, string date = null)
            {
                var assetsList = _bus.QueryAsync(new AssetPairsInfoQuery()).Result;
                var asset = assetsList.Assets.SingleOrDefault(a => a.AssetId == coinId);
                if (asset == null)
                    throw new InvalidOperationException($"Asset {coinId} not registered");
                
                return Resolve(new RetroactiveCommand<CreateWallet>(new CreateWallet(address, coinId), date.ToTime()));
            }

            public bool MineCoin(string address, double amount, string coinId, string blockHash, string date = null)
            {
                var nDate = date.ToTime();
                var assetsList = _bus.QueryAsync(new AssetPairsInfoQuery()).Result;
                var asset = assetsList.Assets.SingleOrDefault(a => a.AssetId == coinId);
                if (asset == null)
                    throw new InvalidOperationException($"Asset {coinId} not registered");

                var result = Resolve(new RetroactiveCommand<MineCoin>(new MineCoin(address, new Quantity(amount, asset), blockHash), nDate));
                return result;
            }

            public bool TransferCoins(string txId, string fromAddress, string toAddress, double amount, double fee, string assetId, string date = null)
            {
                Asset asset = null;
                if (assetId != null)
                {
                    var assetsList = _bus.QueryAsync(new AssetPairsInfoQuery()).Result;
                    asset = assetsList.Assets.SingleOrDefault(a => a.AssetId == assetId);
                    if (asset == null)
                        throw new InvalidOperationException($"Asset {assetId} not registered");
                }

                return Resolve(new RetroactiveCommand<TransferCoins>(new TransferCoins(txId, fromAddress, toAddress, new Quantity(amount, asset), new Quantity(fee, asset)), date.ToTime()));
            }
        }
    }
}