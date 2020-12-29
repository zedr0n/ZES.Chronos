using System.Collections.Generic;
using ZES.Interfaces.Net;

namespace Chronos.Core.Json
{
    public class AddressInfo : IJsonResult
    {
        public string Hash { get; set; }
        public string Balance { get; set; }
        public List<Tx> Txs { get; set; }
        public List<MinedBlock> MinedBlocks { get; set; }
        public string RequestorId { get; set; }
    }
}