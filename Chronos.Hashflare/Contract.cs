using Chronos.Hashflare.Events;
using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Hashflare
{
    public class Contract : EventSourced, IAggregate
    {
        public int Quantity { get; }
        private string _type;

        public Contract()
        {
            Register<HashrateBought>(ApplyEvent);
        }
        
        public Contract(string txId, string type, int quantity, int total, long timestamp)
        {
            Id = txId;
            When(new HashrateBought(txId, type, quantity, total, timestamp));
        }

        public void AddAmountMined(double quantity, long timestamp = default(long))
        {
           // When(new AmountMinedByContract(Id, _type, quantity * Ratio, timestamp)); 
           When(new AmountMinedByContract(Id, _type, quantity, timestamp)); 
        }

        public void Expire(string type, int quantity, long timestamp)
        {
            When(new ContractExpired(Id, type, quantity, timestamp));
        }

        private void ApplyEvent(HashrateBought e)
        {
            _type = e.Type;
        }
    }
}