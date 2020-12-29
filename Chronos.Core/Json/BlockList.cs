using System.Collections.Generic;
using ZES.Infrastructure;
using ZES.Interfaces.Net;

namespace Chronos.Core.Json
{
    public class BlockList : IJsonResult
    {
        public List<Block> Blocks { get; set; }
        public string RequestorId { get; set; }
    }

    public class BlockListV2 : JsonList<BlockV2>
    {
    }

    public class Block
    {
        public List<Tx> Txs { get; set; }
        public string Hash { get; set; }
        public string Miner { get; set; }
        public long Timestamp { get; set; }
    }

    public class BlockV2
    {
        public int Height { get; set; }
        public bool IsMain { get; set; }
        public string Blockhash { get; set; }
        public int TxCount { get; set; }
        public long Blocktimestamp { get; set; }
        public string Miner { get; set; }
        public int UncleCount { get; set; }
    }
}