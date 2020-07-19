using Chronos.Hashflare.Events;
using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Hashflare
{
    /// <summary>
    /// Hashflare aggregate root
    /// </summary>
    public class Hashflare : EventSourced, IAggregate
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Hashflare"/> class.
        /// </summary>
        public Hashflare()
        {
           Register<HashflareRegistered>(ApplyEvent);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Hashflare"/> class.
        /// </summary>
        /// <param name="id">Account id</param>
        /// <param name="username">E-mail username</param>
        public Hashflare(string id, string username)
            : this()
        {
            Id = id;
            When(new HashflareRegistered(username));
        }

        /// <summary>
        /// Gets username / email
        /// </summary>
        public string Username { get; private set; }

        /// <summary>
        /// Add mined coin
        /// </summary>
        /// <param name="type">Coin type</param>
        /// <param name="amount">Coin amount</param>
        public void AddAmountMined(string type, double amount)
        {
            if (amount > 0)
                When(new CoinMined(type, amount));
        }
        
        private void ApplyEvent(HashflareRegistered e)
        {
            Username = e.Username;
        }
    }
}