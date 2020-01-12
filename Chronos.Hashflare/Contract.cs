using Chronos.Hashflare.Events;
using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Hashflare
{
    public class Contract : EventSourced, IAggregate
    {
        public Contract() { }
        public Contract(string txId, string type, int quantity, int total, long timestamp)
        {
            Id = txId;
            base.When(new HashrateBought(txId, type, quantity, total, timestamp));
        }

        public void Expire(string type, int quantity, long timestamp)
        {
            base.When(new ContractExpired(type, quantity, timestamp));
        }
    }
}