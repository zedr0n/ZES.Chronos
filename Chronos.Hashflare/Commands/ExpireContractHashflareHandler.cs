using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Hashflare.Commands
{
    public class ExpireContractHashflareHandler : CommandHandlerBase<ExpireContractHashflare, Hashflare>
    {
        public ExpireContractHashflareHandler(IEsRepository<IAggregate> repository)
            : base(repository) { }

        protected override void Act(Hashflare hashflare, ExpireContractHashflare command) =>
            hashflare.ExpireHashrate(command.Type, command.Quantity, command.Timestamp);
    }
}