/// <filename>
///     AssetPriceQuery.cs
/// </filename>

// <auto-generated/>
 namespace Chronos.Core.Queries
{
  public class SingleAssetPriceQuery : ZES.Infrastructure.Domain.SingleQuery<SingleAssetPrice>
  {
    public string Fordom
    {
       get; 
       set;
    }  
    public SingleAssetPriceQuery() 
    {
    }  
    public SingleAssetPriceQuery(string fordom) : base(fordom) 
    {
      Fordom = fordom;
    }
  }
}

