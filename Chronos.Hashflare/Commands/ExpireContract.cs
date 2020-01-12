using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Hashflare.Commands
{
    public class ExpireContract : Command
    {
        public ExpireContract() { }
        public ExpireContract(string txId, string type, int quantity, long timestamp) 
            : base(txId) 
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
