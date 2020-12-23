/// <filename>
///     AddTransactionQuoteHandler.cs
/// </filename>

// <auto-generated/>
 namespace Chronos.Core.Commands
{
  public class AddTransactionQuoteHandler : ZES.Infrastructure.Domain.CommandHandlerBase<AddTransactionQuote, Transaction>
  {
    public AddTransactionQuoteHandler(ZES.Interfaces.Domain.IEsRepository<ZES.Interfaces.Domain.IAggregate> repository) : base(repository) 
    {
    }  
    protected override void Act (Transaction transaction, AddTransactionQuote command)
    {
      transaction.AddQuote(command.Quantity);
    }
  }
}
