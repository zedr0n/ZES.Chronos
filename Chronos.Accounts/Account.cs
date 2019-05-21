using Chronos.Accounts.Events;
using ZES.Infrastructure.Domain;
using ZES.Interfaces;

namespace Chronos.Accounts
{
    public class Account : EventSourced, IAggregate
    {
        private Type _type;
        private Currency _currency;
        
        public Account()
        {
            Register<AccountCreated>(When);
        }

        public Account(string name, Currency currency, Type type)
            : this()
        {
            base.When(new AccountCreated(name, currency, type));    
        }
        
        public enum Type
        {
            Saving,
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