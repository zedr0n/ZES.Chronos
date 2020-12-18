/// <filename>
///     AssetPriceHandler.cs
/// </filename>

// <auto-generated/>

using NodaTime;

namespace Chronos.Core.Queries
{
  public class AssetPriceHandler : ZES.Interfaces.Domain.IProjectionHandler<AssetPrice>
  {
    public AssetPrice Handle (ZES.Interfaces.IEvent e, AssetPrice state)
    {
      return Handle((dynamic) e, state);;
    }

    public AssetPrice Handle(Chronos.Core.Events.AssetPairRegistered e, AssetPrice state)
    {
      return new AssetPrice(0.0, Instant.MinValue);
    }
    
    public AssetPrice Handle (Chronos.Core.Events.QuoteAdded e, AssetPrice state)
    {
      return new AssetPrice(e.Close, e.Timestamp);
    }
  }
}

