using ZES.Infrastructure.Domain;

namespace Chronos.Hashflare.Events
{
    /// <inheritdoc />
    public class HashflareRegistered : Event
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="HashflareRegistered"/> class.
        /// </summary>
        /// <param name="username">User e-mail</param>
        public HashflareRegistered(string username)
        {
            Username = username;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="HashflareRegistered"/> class.
        /// </summary>
        public HashflareRegistered()
        {
        }

        /// <summary>
        /// Gets username/email
        /// </summary>
        public string Username { get; internal set; }
    }
}