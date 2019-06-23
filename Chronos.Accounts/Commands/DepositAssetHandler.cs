using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;

namespace Chronos.Accounts.Commands
{
    /// <inheritdoc />
    public class DepositAssetHandler : CommandHandlerBase<DepositAsset, Account>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DepositAssetHandler"/> class.
        /// </summary>
        /// <param name="repository">Aggregate root repository</param>
        public DepositAssetHandler(IEsRepository<IAggregate> repository)
            : base(repository)
        {
        }

        /// <inheritdoc />
        protected override void Act(Account root, DepositAsset command) =>
            root.DepositAsset(command.AssetId, command.Quantity);
    }
}