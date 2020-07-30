using System.Reflection;
using Chronos.Coins.Commands;
using Chronos.Coins.Queries;
using SimpleInjector;
using ZES.Infrastructure;
using ZES.Infrastructure.GraphQl;
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

            public CoinInfo CoinInfo(CoinInfoQuery query) => Resolve(query);
            public Stats Stats() => Resolve(new StatsQuery());
        }

        public class Mutation : GraphQlMutation
        {
            public Mutation(IBus bus, ILog log)
                : base(bus, log)
            {
            }

            public bool CreateCoin(CreateCoin command) => Resolve(command);
        }
    }
}