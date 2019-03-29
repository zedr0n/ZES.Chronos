using ZES.Infrastructure.Domain;

namespace Chronos.Coins.Commands
{
    public class CreateCoinCommand : Command
    {
        public string Name { get; set; }
        public string Ticker { get; set; }

        public CreateCoinCommand() {}
        
        public CreateCoinCommand(string name, string ticker)
        {
            AggregateId = name;
            Name = name;
            Ticker = ticker;
        }
    }
}