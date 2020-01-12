using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Hashflare.Commands
{
    public class AddMinedToHashflareHandler : CommandHandlerBase<AddMinedToHashflare, Hashflare>
    {
        public AddMinedToHashflareHandler(IEsRepository<IAggregate> repository)
            : base(repository) { }

        protected override void Act(Hashflare hashflare, AddMinedToHashflare command) =>
            hashflare.AddAmountMined(command.Type, command.Quantity, command.Timestamp);
    }
}