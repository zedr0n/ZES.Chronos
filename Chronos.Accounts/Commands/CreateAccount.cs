using ZES.Infrastructure.Domain;

namespace Chronos.Accounts.Commands
{
    public class CreateAccount : Command
    {
        public CreateAccount() { }

        public CreateAccount(string name, Currency currency, Account.Type type)
        {
            Name = name;
            Currency = currency;
            Type = type;
        }
        
        public string Name 
        { 
            get => Target;
            set => Target = value;
        }

        public Currency Currency { get; set; }
        public Account.Type Type { get; set; }
    }
}