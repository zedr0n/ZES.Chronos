using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Hashflare.Commands
{
    public class ExpireContractHandler : CommandHandlerBase<ExpireContract, Contract>
    {
        public ExpireContractHandler(IEsRepository<IAggregate> repository)
            : base(repository) { }

        protected override void Act(Contract contract, ExpireContract command) =>
            contract.Expire(command.Type, command.Quantity, command.Timestamp);
    }
}