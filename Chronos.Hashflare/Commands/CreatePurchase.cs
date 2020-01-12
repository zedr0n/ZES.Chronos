using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Hashflare.Commands
{
    public class CreatePurchase : Command, ICreateCommand
    {
        public CreatePurchase() { }
        public CreatePurchase(string txId, string type, int quantity, int total, long timestamp) 
            : base(txId) 
        {
            TxId = txId;
            Type = type;
            Quantity = quantity;
            Total = total; 
            Timestamp = timestamp;
        }

        public string TxId { get; }
        public string Type { get; }
        public int Quantity { get; }
        public int Total { get; }
    }
}
