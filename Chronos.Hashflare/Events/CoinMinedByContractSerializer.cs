using Newtonsoft.Json;
using ZES.Infrastructure.Serialization;

namespace Chronos.Hashflare.Events
{
    public class CoinMinedByContractSerializer : EventSerializerBase<CoinMinedByContract>
    {
        public override void Write(JsonTextWriter writer, CoinMinedByContract e)
        {
           writer.WritePropertyName(nameof(CoinMinedByContract.Type));
           writer.WriteValue(e.Type);
           
           writer.WritePropertyName(nameof(CoinMinedByContract.Quantity));
           writer.WriteValue(e.Quantity);
        }
    }
}