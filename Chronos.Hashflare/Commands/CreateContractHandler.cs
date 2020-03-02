using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

#pragma warning disable 1591

namespace Chronos.Hashflare.Commands
{
    public class CreateContractHandler : CreateCommandHandlerBase<CreateContract, Contract>
    {
        public CreateContractHandler(IEsRepository<IAggregate> repository)
            : base(repository) { }

        protected override Contract Create(CreateContract command) => new Contract (
            command.Target, command.Type, command.Quantity, command.Total);
    }
}