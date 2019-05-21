using ZES.Infrastructure.Domain;

namespace Chronos.Accounts.Events
{
    public class AccountCreated : Event
    {
        public AccountCreated(string name, Currency currency, Account.Type type)
        {
            Name = name;
            Currency = currency;
            Type = type;
        }
        
        public string Name { get; }
        public Currency Currency { get; }
        public Account.Type Type { get; }
    }
}