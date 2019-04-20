using ZES.Infrastructure.Domain;

namespace Chronos.Coins.Commands
{
    public class CreateCoin : Command
    {
        public string Name { 
            get => Target;
            set => Target = value;
        }
        public string Ticker { get; set; }

        public CreateCoin() {}
        public CreateCoin(string name, string ticker) : base(name)
        {
            Name = name;
            Ticker = ticker;
        }
    }
}