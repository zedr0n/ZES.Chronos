/// <filename>
///     StartTransferHandler.cs
/// </filename>

// <auto-generated/>
 namespace Chronos.Accounts.Commands
{
  public class StartTransferHandler : ZES.Infrastructure.Domain.CreateCommandHandlerBase<StartTransfer, Transfer>
  {
    public StartTransferHandler(ZES.Interfaces.Domain.IEsRepository<ZES.Interfaces.Domain.IAggregate> repository) : base(repository) 
    {
    }  
    protected override Transfer Create (StartTransfer command)
    {
      return new Transfer(command.TxId, command.FromAccount, command.ToAccount, command.Amount);
    }
  }
}

