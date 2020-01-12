using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Hashflare.Commands
{
    public class ExpireContractHashflare : Command
    {
        public ExpireContractHashflare() { }
        public ExpireContractHashflare(string type, int quantity, long timestamp) 
            : base("Hashflare") 
        {
            Type = type;
            Quantity = quantity;
            Timestamp = timestamp; 
        }

        public string Type { get; }
        public int Quantity { get; }
        public int Total { get; }
    }
}
