/// <filename>
///     RegisterAssetPairHandler.cs
/// </filename>

// <auto-generated/>
 namespace Chronos.Core.Commands
{
  public class RegisterAssetPairHandler : ZES.Infrastructure.Domain.CreateCommandHandlerBase<RegisterAssetPair, AssetPair>
  {
    public RegisterAssetPairHandler(ZES.Interfaces.Domain.IEsRepository<ZES.Interfaces.Domain.IAggregate> repository) : base(repository) 
    {
    }  
    protected override AssetPair Create (RegisterAssetPair command)
    {
      return new AssetPair(command.Fordom, command.ForAsset, command.DomAsset);
    }
  }
}
