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
            base.When(new HashflareRegistered(username, timestamp));
        }

        public string Username { get; private set; }
        
        private void ApplyEvent(HashflareRegistered e)
        {
            Username = e.Username;
        }
    }
}