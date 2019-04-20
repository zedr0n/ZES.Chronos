using Chronos.Coins.Commands;
using Chronos.Coins.Queries;

namespace Chronos.Coins
{
    public static class Schema
    {
        public class Query
        {
            public CoinInfo CoinInfo(CoinInfoQuery query) => null;
            public Stats Stats(StatsQuery query) => null;
        }

        public class Mutation
        {
            public bool CreateCoin(CreateCoin command) => true;
        }
    }
}