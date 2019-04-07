using ZES.Infrastructure.Domain;

namespace Chronos.Coins.Commands
{
    public class CreateCoin : Command
    {
        public string Name { get; }
        public string Ticker { get; }

        public CreateCoin(string name, string ticker) : base(name)
        {
            Name = name;
            Ticker = ticker;
        }
    }
}