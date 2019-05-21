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
        /// <param name="currency">Account currency</param>
        /// <param name="type">Account type</param>
        public AccountCreated(string name, Currency currency, Account.Type type)
        {
            Name = name;
            Currency = currency;
            Type = type;
        }

        /// <summary>
        /// Gets account name ( identifier )
        /// </summary>
        /// <value>
        /// <placeholder>Account name ( identifier )</placeholder>
        /// </value>
        public string Name { get; }
        
        /// <summary>
        /// Gets or sets account currency
        /// </summary>
        /// <value>
        /// <placeholder>Account currency</placeholder>
        /// </value>
        public Currency Currency { get; set; }

        /// <summary>
        /// Gets or sets account type 
        /// </summary>
        /// <value>
        /// <placeholder>Account type </placeholder>
        /// </value>
        public Account.Type Type { get; set; }
    }
}