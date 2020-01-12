using ZES.Infrastructure.Domain;

namespace Chronos.Hashflare.Events
{
    public class HashrateBought : Event
    {
        public HashrateBought(string txId, string type, int quantity, int total, long timestamp)
        {
            Timestamp = timestamp;
            Type = type;
            Quantity = quantity;
            Total = total;
            TxId = txId;
        }

        public string TxId { get; }
        public string Type { get; }
        public int Quantity { get; }
        public int Total { get; }
    }
}