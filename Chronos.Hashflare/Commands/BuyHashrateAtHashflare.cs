using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Hashflare.Commands
{
    public class BuyHashrateAtHashflare : Command
    {
        public BuyHashrateAtHashflare() { }
        public BuyHashrateAtHashflare(string type, int quantity, int total, long timestamp) 
            : base("Hashflare") 
        {
            Type = type;
            Quantity = quantity;
            Total = total; 
            Timestamp = timestamp;
        }

        public string Type { get; }
        public int Quantity { get; }
        public int Total { get; }
    }
}
