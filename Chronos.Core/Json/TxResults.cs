using ZES.Infrastructure;
using ZES.Interfaces.Net;

namespace Chronos.Core.Json
{
    public class TxResults : JsonList<Tx>, IJsonResult
    { }

    public class Tx
    {
        public string Hash { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public double Amount { get; set; }
        public long ReceiveTime { get; set; }
    }
}