using Chronos.Hashflare.Events;
using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Hashflare
{
    public class Purchase : EventSourced, IAggregate
    {
        public Purchase() { }
        public Purchase(string txId, string type, int quantity, int total, long timestamp)
        {
            Id = txId;
            base.When(new HashrateBought(txId, type, quantity, total, timestamp));
        }
    }
}