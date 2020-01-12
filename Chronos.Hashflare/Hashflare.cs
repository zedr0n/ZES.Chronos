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
           Register<HashrateBought>(ApplyEvent);
           Register<ContractExpired>(ApplyEvent);
        }
        
        public Hashflare(string id, string username, long timestamp)
            : this()
        {
            Id = id;
            base.When(new HashflareRegistered(username, timestamp));
        }

        public int BitcoinHashRate { get; private set; }
        public string Username { get; private set; }
        
        public void BuyHashrate(string type, int quantity, int total, long timestamp)
        {
            base.When(new HashrateBought(string.Empty, type, quantity, total, timestamp));
        }

        public void ExpireHashrate(string type, int quantity, long timestamp)
        {
            base.When(new ContractExpired(type, quantity, timestamp));
        }

        private void ApplyEvent(HashflareRegistered e)
        {
            Username = e.Username;
        }

        private void ApplyEvent(HashrateBought e)
        {
            if (e.Type == "SHA-256")
                BitcoinHashRate += e.Quantity;
        }

        private void ApplyEvent(ContractExpired e)
        {
            if (e.Type == "SHA-256")
                BitcoinHashRate -= e.Quantity;
        }
    }
}