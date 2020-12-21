/// <filename>
///     AssetPriceHandler.cs
/// </filename>

// <auto-generated/>

using NodaTime;

namespace Chronos.Core.Queries
{
  public class SingleAssetPriceHandler : ZES.Interfaces.Domain.IProjectionHandler<SingleAssetPrice>
  {
    public SingleAssetPrice Handle (ZES.Interfaces.IEvent e, SingleAssetPrice state)
    {
      return Handle((dynamic) e, state);;
    }

    public SingleAssetPrice Handle(Chronos.Core.Events.AssetPairRegistered e, SingleAssetPrice state)
    {
      return new SingleAssetPrice(0.0, Instant.MinValue);
    }
    
    public SingleAssetPrice Handle (Chronos.Core.Events.QuoteAdded e, SingleAssetPrice state)
    {
      return new SingleAssetPrice(e.Close, e.Timestamp);
    }
  }
}
