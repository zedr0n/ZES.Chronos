using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;
using ZES.Interfaces.EventStore;

namespace Chronos.Accounts.Commands;

public class TransactAssetHandler(IEsRepository<IAggregate> repository)
    : CommandHandlerBase<TransactAsset, Account>(repository)
{
    protected override void Act(Account root, TransactAsset command)
    {
        root.TransactAsset(command.Asset, command.Cost, command.Fee, command.CreateOffsettingCostTransaction);
    }
}