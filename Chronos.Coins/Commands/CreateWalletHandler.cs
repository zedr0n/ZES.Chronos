/// <filename>
///     CreateWalletHandler.cs
/// </filename>

// <auto-generated/>
 namespace Chronos.Coins.Commands
{
  public class CreateWalletHandler : ZES.Infrastructure.Domain.CreateCommandHandlerBase<CreateWallet, Wallet>
  {
    public CreateWalletHandler(ZES.Interfaces.Domain.IEsRepository<ZES.Interfaces.Domain.IAggregate> repository) : base(repository) 
    {
    }  
    protected override Wallet Create (CreateWallet command)
    {
      return new Wallet(command.Address, command.Coin);
    }
  }
}

