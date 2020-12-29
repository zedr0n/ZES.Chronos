using System.Collections.Generic;
using ZES.Interfaces.Net;

namespace Chronos.Core.Json
{
    public class BlockInfoV2 : IJsonResult
    {
        public int Height { get; set; }
        public string Miner { get; set; }
        public List<TxV2> Txs { get; set; }
        public List<UncleV2> Uncles { get; set; }
        public string RequestorId { get; set; }
    }

    public class TxV2
    {
        public string TxHash { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public double Amount { get; set; }
        public long BlockTimeStamp { get; set; }
    }

    public class UncleV2
    {
        public int Height { get; set; }
        public int Depth { get; set; }
        public string Miner { get; set; }
        public string UncleHash { get; set; }
        public long UncleTimeStamp { get; set; }
    }
}