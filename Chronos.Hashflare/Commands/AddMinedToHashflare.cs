using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Hashflare.Commands
{
    public class AddMinedToHashflare : Command
    {
        public AddMinedToHashflare() { }
        public AddMinedToHashflare(string type, double quantity, long timestamp) 
            : base("Hashflare") 
        {
            Type = type;
            Quantity = quantity;
            Timestamp = timestamp; 
        }

        public string Type { get; }
        public double Quantity { get; }
    }
}
