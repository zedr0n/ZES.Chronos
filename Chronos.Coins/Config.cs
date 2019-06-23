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
        public abstract class Query
        {
            public abstract CoinInfo CoinInfo(CoinInfoQuery query);
            public abstract Stats Stats(StatsQuery query);
        }

        [RootMutation]
        public abstract class Mutation
        {
            public abstract bool CreateCoin(CreateCoin command);
        }
    }
}