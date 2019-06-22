using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Accounts.Commands
{
    /// <summary>
    /// Command to create account
    /// </summary>
    public class CreateAccount : Command, ICreateCommand
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CreateAccount"/> class.
        /// </summary>
        public CreateAccount() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="CreateAccount"/> class.
        /// </summary>
        /// <param name="name">Account identifier</param>
        /// <param name="type">Account type</param>
        public CreateAccount(string name, Account.Type type)
        {
            Name = name;
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
        /// Gets or sets account type 
        /// </summary>
        /// <value>
        /// <placeholder>Account type </placeholder>
        /// </value>
        public Account.Type Type { get; set; }
    }
}