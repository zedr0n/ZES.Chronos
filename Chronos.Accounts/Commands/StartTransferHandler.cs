using ZES.Infrastructure.Domain;
using ZES.Interfaces.Domain;
using ZES.Interfaces.EventStore;

namespace Chronos.Accounts.Commands;

public class StartTransferHandler(IEsRepository<IAggregate> repository)
    : CreateCommandHandlerBase<StartTransfer, Transfer>(repository)
{
    protected override Transfer Create (StartTransfer command)
    {
        return new Transfer(command.TxId, command.FromAccount, command.ToAccount, command.Amount, command.Fee);
    }
}

