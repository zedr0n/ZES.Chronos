using System.Threading.Tasks;
using ZES.Interfaces.Domain;

namespace Chronos.Coins.Commands
{
    public class CreateCoinHandler : ICommandHandler<CreateCoinCommand>
    {
        private readonly IDomainRepository _domainRepository;

        public CreateCoinHandler(IDomainRepository domainRepository)
        {
            _domainRepository = domainRepository;
        }

        public async Task Handle(CreateCoinCommand command)
        {
            var coin = new Coin(command.Ticker,command.Name);
            await _domainRepository.Save(coin);
        }
    }    
}