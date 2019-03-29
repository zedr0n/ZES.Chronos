using System;
using ZES.Interfaces.Domain;

namespace Coins.Queries
{
    public class CoinInfoQuery : IQuery<CoinInfoQuery>
    {
        public string Name { get; set; }
        public Guid CoinId { get; set; }
    }
}