using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Hashflare.Commands
{
    public class BuyHashrateAtHashflareHandler : CommandHandlerBase<BuyHashrateAtHashflare, Hashflare>
    {
        public BuyHashrateAtHashflareHandler(IEsRepository<IAggregate> repository)
            : base(repository) { }

        protected override void Act(Hashflare hashflare, BuyHashrateAtHashflare command) =>
            hashflare.BuyHashrate(command.Type, command.Quantity, command.Total, command.Timestamp); 
    }
}