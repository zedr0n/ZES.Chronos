using System;
using ZES.Infrastructure;
using ZES.Interfaces.Domain;

namespace Chronos.Coins.Queries
{
    public class CoinInfoQuery : Query<CoinInfo> 
    {
        public string Name { get; }

        public CoinInfoQuery(string name)
        {
            Name = name;
        }
    }
}