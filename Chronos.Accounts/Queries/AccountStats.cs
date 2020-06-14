using System.Threading;
using ZES.Interfaces.Domain;

namespace Chronos.Accounts.Queries
{
    /// <summary>
    /// Account stats
    /// </summary>
    public class AccountStats : IState
    {
        private int _numberOfAccounts;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="AccountStats"/> class.
        /// </summary>
        public AccountStats() { }

        /// <summary>
        /// Gets number of accounts 
        /// </summary>
        public int NumberOfAccounts => _numberOfAccounts;

        /// <summary>
        /// Atomic increment the number of accounts
        /// </summary>
        public void Increment()
        {
            Interlocked.Increment(ref _numberOfAccounts);
        }
    }
}