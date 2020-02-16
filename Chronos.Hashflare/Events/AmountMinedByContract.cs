using ZES.Infrastructure.Domain;

namespace Chronos.Hashflare.Events
{
    public class AmountMinedByContract : Event
    {
        public AmountMinedByContract(string txId, string type, double quantity, long timestamp)
        {
            TxId = txId;
            Type = type;
            Quantity = quantity;
            Timestamp = timestamp;
        }

        public string TxId { get; }
        public string Type { get; }
        public double Quantity { get; }
    }
}