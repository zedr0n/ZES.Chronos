using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Hashflare.Commands
{
    public class AddMinedToContractHandler : CommandHandlerBase<AddMinedToContract, Contract>
    {
        public AddMinedToContractHandler(IEsRepository<IAggregate> repository)
            : base(repository) { }

        protected override void Act(Contract contract, AddMinedToContract command) =>
            contract.AddAmountMined(command.Quantity, command.Timestamp);
    }
}