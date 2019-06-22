using System.Reflection;
using Chronos.Coins.Commands;
using Chronos.Coins.Queries;
using SimpleInjector;
using ZES.Infrastructure.Attributes;
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
        
        [RootQuery]
        public class Query
        {
            public CoinInfo CoinInfo(CoinInfoQuery query) => null;
            public Stats Stats(StatsQuery query) => null;
        }

        [RootMutation]
        public class Mutation
        {
            public bool CreateCoin(CreateCoin command) => true;
        }
    }
}