using System.Reflection;
using Chronos.Coins.Commands;
using Chronos.Coins.Queries;
using SimpleInjector;
using ZES;
using ZES.Infrastructure.Attributes;

namespace Chronos.Coins
{
    public static class Config
    {
        
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
        
        [Registration]
        public static void RegisterAll(Container c)
        {
            c.RegisterAll(Assembly.GetExecutingAssembly());
        }
    }
}