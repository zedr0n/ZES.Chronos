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
using ZES.Interfaces;
using ZES.Interfaces.Pipes;
using ZES.Utils;

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
            public Stats Stats(string date = null) => Resolve(new StatsQuery { Timestamp = date?.ToInstant()?.Success ?? false ? date.ToInstant().Value : default });
            public WalletInfo WalletInfo(string address, string date = null) => Resolve(new WalletInfoQuery(address) { Timestamp = date?.ToInstant()?.Success ?? false ? date.ToInstant().Value : default });
        }

        public class Mutation : GraphQlMutation
        {
            private readonly IBus _bus;
            
            public Mutation(IBus bus, ILog log)
                : base(bus, log)
            {
                _bus = bus;
            }

            public bool CreateCoin(string coin, string ticker) => Resolve(new CreateCoin(coin, ticker));

            public bool CreateWallet(string address, string coinId, string date = null)
            {
                var nDate = date.ToInstant();
                if (!nDate.Success)
                    return false;
                return Resolve(new RetroactiveCommand<CreateWallet>(new CreateWallet(address, coinId), nDate.Value)); 
            }

            public bool TransferCoins(string txId, string fromAddress, string toAddress, double amount, double fee, string assetId, string date = null)
            {
                Asset asset = null;
                if (assetId != null)
                {
                    var assetsList = _bus.QueryAsync(new AssetPairsInfoQuery()).Result;
                    asset = assetsList.Assets.SingleOrDefault(a => a.AssetId == assetId);
                    if (asset == null)
                        return false;
                }

                var nDate = date.ToInstant();
                if (!nDate.Success)
                    return false;
                
                return Resolve(new RetroactiveCommand<TransferCoins>(new TransferCoins(txId, fromAddress, toAddress, new Quantity(amount, asset), new Quantity(fee, asset)), nDate.Value));
            }
        }
    }
}