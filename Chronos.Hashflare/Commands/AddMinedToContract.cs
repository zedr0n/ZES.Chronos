using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Hashflare.Commands
{
    public class AddMinedToContract : Command
    {
        public AddMinedToContract() { }
        public AddMinedToContract(string txId, string type, double quantity, long timestamp = default(long)) 
            : base(txId) 
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
