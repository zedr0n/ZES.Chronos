using ZES.Infrastructure.Domain;

namespace Chronos.Accounts.Events
{
    /// <summary>
    /// Event when new account is created
    /// </summary>
    public class AccountCreated : Event
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AccountCreated"/> class.
        /// </summary>
        /// <param name="name">Account identifier</param>
        /// <param name="type">Account type</param>
        public AccountCreated(string name,  Account.Type type)
        {
            Name = name;
            Type = type;
        }

        /// <summary>
        /// Gets account name ( identifier )
        /// </summary>
        /// <value>Account name ( identifier )</value>
        public string Name { get; }
        
        /// <summary>
        /// Gets or sets account type 
        /// </summary>
        /// <value>
        /// Account type 
        /// </value>
        public Account.Type Type { get; set; }
    }
}