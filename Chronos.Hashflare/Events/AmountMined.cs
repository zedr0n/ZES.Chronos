using ZES.Infrastructure.Domain;

namespace Chronos.Hashflare.Events
{
    public class AmountMined : Event
    {
        public AmountMined(string type, double quantity, long timestamp)
        {
            Type = type;
            Quantity = quantity;
            Timestamp = timestamp;
        }

        public string Type { get; }
        public double Quantity { get; }
    }
}