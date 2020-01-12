using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Hashflare.Commands
{
    public class AdjustRatioForContractHandler : CommandHandlerBase<AdjustRatioForContract, Contract>
    {
        public AdjustRatioForContractHandler(IEsRepository<IAggregate> repository)
            : base(repository) { }

        protected override void Act(Contract contract, AdjustRatioForContract command) =>
            contract.AdjustRatio(command.Ratio, command.Timestamp);
    }
}