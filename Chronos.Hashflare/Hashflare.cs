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
        
        public Hashflare(string id, string username)
            : this()
        {
            Id = id;
            base.When(new HashflareRegistered(username));
        }
        
        public string Username { get; private set; }

        private void ApplyEvent(HashflareRegistered e)
        {
            Username = e.Username;
        }
    }
}