/// <filename>
///     RegisterAssetPair.cs
/// </filename>

// <auto-generated/>
 namespace Chronos.Core.Commands
{
  public class RegisterAssetPair : ZES.Infrastructure.Domain.Command, ZES.Interfaces.Domain.ICreateCommand
  {
    public RegisterAssetPair() 
    {
    }  
    public string Fordom
    {
       get; 
       set;
    }  
    public Asset ForAsset
    {
       get; 
       set;
    }  
    public Asset DomAsset
    {
       get; 
       set;
    }  
    public override string Target
    {
       get
      {
        return Fordom;
      }
    }  
    public RegisterAssetPair(string fordom, Asset forAsset, Asset domAsset) 
    {
      Fordom = fordom; 
      ForAsset = forAsset; 
      DomAsset = domAsset;
    }
  }
}
