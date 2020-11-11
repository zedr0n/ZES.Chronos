/// <filename>
///     AddMinedCoinToContractHandler.cs
/// </filename>

// <auto-generated/>
 namespace Chronos.Hashflare.Commands
{
  public class AddMinedCoinToContractHandler : ZES.Infrastructure.Domain.CommandHandlerBase<AddMinedCoinToContract, Contract>
  {
    public AddMinedCoinToContractHandler(ZES.Interfaces.Domain.IEsRepository<ZES.Interfaces.Domain.IAggregate> repository) : base(repository) 
    {
    }  
    protected override void Act (Contract contract, AddMinedCoinToContract command)
    {
      contract.AddAmountMined(command.Type, command.Quantity);
    }
  }
}

