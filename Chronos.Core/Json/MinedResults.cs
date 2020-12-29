using ZES.Infrastructure;

namespace Chronos.Core.Json
{
    public class MinedResults : JsonList<MinedBlock>
    {
    }

    public class MinedBlock
    {
        public string Blockhash { get; set; }
        public long Timestamp { get; set; }
        public string Miner { get; set; }
        public double Feereward { get; set; }
    }
}