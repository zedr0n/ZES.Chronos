using ZES.Infrastructure.Domain;

namespace Chronos.Hashflare.Events
{
    public class HashflareRegistered : Event
    {
        public HashflareRegistered(string username)
        {
            Username = username;
        }
        
        public string Username { get; }
    }
}