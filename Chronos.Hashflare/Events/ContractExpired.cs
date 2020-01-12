using ZES.Infrastructure.Domain;

namespace Chronos.Hashflare.Events
{
    public class ContractExpired : Event
    {
        public ContractExpired(string type, int quantity, long timestamp)
        {
            Type = type;
            Quantity = quantity;
            Timestamp = timestamp;
        }

        public string Type { get; }
        public int Quantity { get; }
    }
}