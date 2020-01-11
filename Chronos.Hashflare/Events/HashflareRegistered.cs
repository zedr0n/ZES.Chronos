using ZES.Infrastructure.Domain;

namespace Chronos.Hashflare.Events
{
    public class HashflareRegistered : Event
    {
        public HashflareRegistered(string username, long timestamp)
        {
            Username = username;
            Timestamp = timestamp;
        }
        
        public string Username { get; }
    }
}