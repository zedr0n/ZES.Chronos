using Chronos.Accounts.Events;
using ZES.Infrastructure.Domain;
using ZES.Interfaces;

namespace Chronos.Accounts
{
    /// <inheritdoc cref="EventSourced" />
    public class Account : EventSourced, IAggregate
    {
        private Type _type;
        private Currency _currency;

        /// <summary>
        /// Initializes a new instance of the <see cref="Account"/> class.
        /// </summary>
        public Account()
        {
            Register<AccountCreated>(When);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Account"/> class.
        /// </summary>
        /// <param name="name">Account identifier</param>
        /// <param name="currency">Account currency</param>
        /// <param name="type">Account type</param>
        public Account(string name, Currency currency, Type type)
            : this()
        {
            base.When(new AccountCreated(name, currency, type));    
        }
        
        /// <summary>
        /// Account type enum
        /// </summary>
        public enum Type
        {
            /// <summary>
            /// Savings account ( bank, cash, etc... )
            /// </summary>
            Saving,
            
            /// <summary>
            /// Trading account ( stocks, crypto, etc... ) 
            /// </summary>
            Trading
        }

        private void When(AccountCreated e)
        {
            Id = e.Name;
            _type = e.Type;
            _currency = e.Currency;
        }
    }
}