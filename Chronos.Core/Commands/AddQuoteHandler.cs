/// <filename>
///     AddQuoteHandler.cs
/// </filename>

// <auto-generated/>
 namespace Chronos.Core.Commands
{
  public class AddQuoteHandler : ZES.Infrastructure.Domain.CommandHandlerBase<AddQuote, AssetPair>
  {
    public AddQuoteHandler(ZES.Interfaces.Domain.IEsRepository<ZES.Interfaces.Domain.IAggregate> repository) : base(repository) 
    {
    }  
    protected override void Act (AssetPair assetPair, AddQuote command)
    {
      assetPair.AddQuote(command.Date, command.Close, command.Open, command.Low, command.High);
    }
  }
}

