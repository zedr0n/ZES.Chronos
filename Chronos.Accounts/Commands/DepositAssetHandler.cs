using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Accounts.Commands
{
    public class DepositAssetHandler : CommandHandlerBase<DepositAsset, Account>
    {
        public DepositAssetHandler(IEsRepository<IAggregate> repository)
            : base(repository)
        {
        }

        /// <inheritdoc />
        protected override void Act(Account root, DepositAsset command) =>
            root.DepositAsset(command.AssetId, command.Quantity);
    }
}