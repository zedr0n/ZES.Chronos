using ZES.Infrastructure.Domain;

namespace Chronos.Hashflare.Events
{
    public class ContractExpired : Event
    {
        public ContractExpired(string txId, string type, int quantity, long timestamp)
        {
            Type = type;
            Quantity = quantity;
            Timestamp = timestamp;
            TxId = txId;
        }

        public string TxId { get; }
        public string Type { get; }
        public int Quantity { get; }
    }
}