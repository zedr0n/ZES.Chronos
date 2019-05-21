using ZES.Infrastructure.Domain;

namespace Chronos.Accounts.Commands
{
    /// <summary>
    /// Command to create account
    /// </summary>
    public class CreateAccount : Command
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateAccount"/> class.
        /// </summary>
        public CreateAccount() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateAccount"/> class.
        /// </summary>
        /// <param name="name">Account identifier</param>
        /// <param name="currency">Account currency</param>
        /// <param name="type">Account type</param>
        public CreateAccount(string name, Currency currency, Account.Type type)
        {
            Name = name;
            Currency = currency;
            Type = type;
        }

        /// <summary>
        /// Gets or sets account name ( identifier )
        /// </summary>
        /// <value>
        /// Account name ( identifier )
        /// </value>
        public string Name 
        { 
            get => Target;
            set => Target = value;
        }

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