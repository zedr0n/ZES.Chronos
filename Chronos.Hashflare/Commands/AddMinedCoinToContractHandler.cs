using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

#pragma warning disable 1591

namespace Chronos.Hashflare.Commands
{
    public class AddMinedCoinToContractHandler : CommandHandlerBase<AddMinedCoinToContract, Contract>
    {
        public AddMinedCoinToContractHandler(IEsRepository<IAggregate> repository)
            : base(repository) { }

        protected override void Act(Contract contract, AddMinedCoinToContract command) =>
            contract.AddAmountMined(command.Quantity);
    }
}