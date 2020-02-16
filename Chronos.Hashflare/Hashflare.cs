using Chronos.Hashflare.Events;
using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Hashflare
{
    public class Hashflare : EventSourced, IAggregate
    {
        public Hashflare()
        {
           Register<HashflareRegistered>(ApplyEvent);
        }
        
        public Hashflare(string id, string username, long timestamp)
            : this()
        {
            Id = id;
            When(new HashflareRegistered(username, timestamp));
        }

        public string Username { get; private set; }

        public void AddAmountMined(string type, double amount, long timestamp)
        {
            if (amount > 0)
                When(new AmountMined(type, amount, timestamp));
        }
        
        private void ApplyEvent(HashflareRegistered e)
        {
            Username = e.Username;
        }
    }
}