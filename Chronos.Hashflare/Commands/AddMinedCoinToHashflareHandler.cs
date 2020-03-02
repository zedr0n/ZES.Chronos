using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

#pragma warning disable 1591

namespace Chronos.Hashflare.Commands
{
    public class AddMinedCoinToHashflareHandler : CommandHandlerBase<AddMinedCoinToHashflare, Hashflare>
    {
        public AddMinedCoinToHashflareHandler(IEsRepository<IAggregate> repository)
            : base(repository) { }

        protected override void Act(Hashflare hashflare, AddMinedCoinToHashflare command) =>
            hashflare.AddAmountMined(command.Type, command.Quantity);
    }
}