using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Hashflare.Commands
{
    /// <summary>
    /// Register hashflare command
    /// </summary>
    public class RegisterHashflare : Command, ICreateCommand   
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterHashflare"/> class.
        /// </summary>
        public RegisterHashflare() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="RegisterHashflare"/> class.
        /// </summary>
        /// <param name="username">Username / email</param>
        public RegisterHashflare(string username)
            : base("Hashflare")
        {
            Username = username;
        }
        
        /// <summary>
        /// Gets username/email
        /// </summary>
        public string Username { get; }
    }
}
