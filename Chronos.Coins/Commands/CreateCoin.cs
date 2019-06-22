using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Coins.Commands
{
    public class CreateCoin : Command, ICreateCommand
    {
        public CreateCoin() { }
        public CreateCoin(string name, string ticker)
            : base(name)
        {
            Name = name;
            Ticker = ticker;
        }
        
        public string Name 
        { 
            get => Target;
            set => Target = value;
        }
        
        public string Ticker { get; set; }
    }
}