using System;
using Chronos.Coins.Projections;
using ZES.Interfaces.Domain;

namespace Chronos.Coins.Queries
{
    public class CoinInfoQuery : IQuery<CoinInfo>
    {
        public string Name { get; }

        public CoinInfoQuery(string name)
        {
            Name = name;
        }
    }
}