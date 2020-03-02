using ZES.Infrastructure.Domain;

namespace Chronos.Hashflare.Events
{
    /// <inheritdoc />
    public class HashflareRegistered : Event
    {
        /// <inheritdoc />
        public HashflareRegistered(string username)
        {
            Username = username;
        }
        
        /// <summary>
        /// Gets username/email
        /// </summary>
        public string Username { get; }
    }
}