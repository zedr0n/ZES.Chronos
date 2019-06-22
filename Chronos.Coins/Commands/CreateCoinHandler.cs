using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Coins.Commands
{
    public class CreateCoinHandler : CreateCommandHandlerBase<CreateCoin, Coin>
    {
        public CreateCoinHandler(IEsRepository<IAggregate> repository)
            : base(repository)
        {
        }

        protected override Coin Create(CreateCoin command) => new Coin(command.Ticker, command.Name);
    }
}